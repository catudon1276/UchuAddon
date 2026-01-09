using BepInEx;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Nebula;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Ghost.Complex;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
[NebulaRPCHolder]
public static class AchievementLoader
{
    private static readonly HashSet<string> LoadedAddons = new();
    private static readonly HashSet<string> LoadedAchievementIds = new();
    private static IResourceAllocator AsResource(this NebulaAddon addon) => addon;

    private static System.Collections.IEnumerator Preprocess(NebulaPreprocessor preprocessor)
    {
        yield return preprocessor.SetLoadingText("Load Custom Achievements");

        foreach (var addon in NebulaAddon.AllAddons)
        {
            // 跳过已加载的addon
            if (LoadedAddons.Contains(addon.Id))
            {
                continue;
            }

            // 检查资源文件
            var resource = addon.AsResource().GetResource("CustomAchievement.dat");
            if (resource == null)
            {
                
                continue;
            }

            

            try
            {
                using (var stream = resource.AsStream())
                {
                    if (stream == null)
                    {
                      
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        List<ProgressRecord> recordsList = new();
                        List<AchievementType> types = new();

                        while (true)
                        {
                            types.Clear();

                            var line = reader.ReadLine();
                            if (line == null) break;

                            if (line.StartsWith("#")) continue;

                            var args = line.Split(',');

                            if (args.Length < 2) continue;

                            // 添加addon前缀确保ID唯一性
                            string achievementId = $"{args[0]}";

                            if (LoadedAchievementIds.Contains(achievementId))
                            {
                              
                                continue;
                            }

                            bool clearOnce = false;
                            bool noHint = false;
                            bool secret = false;
                            bool isNotChallenge = false;
                            bool isRecord = false;
                            bool innersloth = false;
                            bool unlock = false;
                            bool hasPrefix = false;
                            bool hasPostfix = false;
                            bool hasInfix = false;
                            string? reference = null;
                            string? defaultSource = null;
                            string? preAchievement = null;
                            int attention = 0;
                            IEnumerable<ProgressRecord>? records = recordsList;

                            IEnumerable<DefinedAssignable> relatedRoles = [];
                            Image? specifiedImage = null;

                            int rarity = int.Parse(args[1]);
                            int goal = 1;
                            for (int i = 2; i < args.Length - 1; i++)
                            {
                                var arg = args[i];

                                switch (arg)
                                {
                                    case "once":
                                        clearOnce = true;
                                        break;
                                    case "noHint":
                                        noHint = true;
                                        break;
                                    case "secret":
                                        secret = true;
                                        break;
                                    case "seasonal":
                                        types.Add(AchievementType.Seasonal);
                                        break;
                                    case "costume":
                                        types.Add(AchievementType.Costume);
                                        break;
                                    case "perk":
                                        types.Add(AchievementType.Perk);
                                        break;
                                    case "nonChallenge":
                                        isNotChallenge = true;
                                        break;
                                    case "innersloth":
                                        innersloth = true;
                                        break;
                                    case "sp0":
                                        hasPrefix = true;
                                        break;
                                    case "sp1":
                                        hasInfix = true;
                                        break;
                                    case "sp2":
                                        hasPostfix = true;
                                        break;
                                    case "unlock":
                                        unlock = true;
                                        break;
                                    case string a when a.StartsWith("goal-"):
                                        goal = int.Parse(a.Substring(5));
                                        break;
                                    case string a when a.StartsWith("sync-"):
                                        reference = a.Substring(5);
                                        break;
                                    case string a when a.StartsWith("default-"):
                                        defaultSource = a.Substring(8);
                                        break;
                                    case string a when a.StartsWith("a-"):
                                        if (int.TryParse(a.AsSpan(2), out var val)) attention = val;
                                        break;
                                    case string a when a.StartsWith("image-role-"):
                                        var roleName = a.Substring(11);
                                        specifiedImage = Roles.AllAssignables().FirstOrDefault(a => a.LocalizedName == roleName)?.ConfigurationHolder?.Illustration;
                                        break;
                                    case string a when a.StartsWith("image-combi-"):
                                        var combiName = a.Substring(12);
                                        specifiedImage = CombiImageInfo.FastImages.TryGetValue(combiName.HeadUpper(), out var combiInfo) ? combiInfo.Image : null;
                                        break;
                                    case string a when a.StartsWith("collab-"):
                                        if (AchievementType.TryGetCollabType(a.Substring(7), out var aType)) types.Add(aType);
                                        break;
                                    case string a when a.StartsWith("pre-"):
                                        preAchievement = a.Substring(4);
                                        break;
                                }
                            }

                            if (secret) types.Add(AchievementType.Secret);

                            var nameSplitted = args[0].Split('.');
                            if (nameSplitted.Length > 1)
                            {
                                if (nameSplitted[0] == "combination" && nameSplitted.Length > 2 && int.TryParse(nameSplitted[1], out var num) && nameSplitted.Length >= 2 + num)
                                {
                                    relatedRoles = Helpers.Sequential(num).Select(i =>
                                    {
                                        var roleName = nameSplitted[2 + i].Replace('-', '.');
                                        return Roles.AllAssignables().FirstOrDefault(a => a.LocalizedName == roleName);
                                    }).Where(r => r != null).ToArray()!;
                                    if (rarity == 2 && !isNotChallenge) types.Add(AchievementType.Challenge);
                                }
                                else
                                {
                                    nameSplitted[0] = nameSplitted[0].Replace('-', '.');
                                    var cand = Roles.AllAssignables().FirstOrDefault(a => a.LocalizedName == nameSplitted[0]);
                                    if (cand != null)
                                    {
                                        relatedRoles = [cand];
                                        if (rarity == 2 && !isNotChallenge)
                                        {
                                            types.Add(AchievementType.Challenge);
                                            if (attention < 80) attention = 80;
                                        }
                                    }
                                }
                            }

                            // 创建新成就
                            if (innersloth)
                            {
                                var innerslothAchievement=new InnerslothAchievement(noHint, achievementId, specifiedImage);
                                innerslothAchievement.HasInfix = hasInfix;
                                innerslothAchievement.HasPrefix = hasPrefix;
                                innerslothAchievement.HasSuffix = hasPostfix;
                            }
                            else if (isRecord)
                            {
                                new DisplayProgressRecord(achievementId, goal, "record." + achievementId, defaultSource);
                            }
                            else if (!records.IsEmpty<ProgressRecord>()||unlock)
                            {
                                if (records.IsEmpty())
                                {
                                    records = new ProgressRecord[] { new ProgressRecord(null,"unlock." + achievementId, 0, true) };
                                }
                                var completeAchievement=new CompleteAchievement(records.ToArray(), secret, noHint, achievementId, relatedRoles, types.ToArray(), rarity, attention, specifiedImage,preAchievement);
                                completeAchievement.HasInfix = hasInfix;
                                completeAchievement.HasPrefix = hasPrefix;
                                completeAchievement.HasSuffix = hasPostfix;
                            }
                            else if (reference != null)
                            {
                                var sumUpReferenceAchievement = new SumUpReferenceAchievement(secret, achievementId, reference, goal, relatedRoles, types.ToArray(), rarity, attention, specifiedImage,preAchievement);
                                sumUpReferenceAchievement.HasInfix = hasInfix;
                                sumUpReferenceAchievement.HasPrefix = hasPrefix;
                                sumUpReferenceAchievement.HasSuffix = hasPostfix;
                            }
                            else if (goal > 1)
                            {
                                var sumUpAchievement=new SumUpAchievement(secret, noHint, achievementId, goal, relatedRoles, types.ToArray(), rarity, attention, specifiedImage,preAchievement);
                                sumUpAchievement.HasInfix = hasInfix;
                                sumUpAchievement.HasPrefix = hasPrefix;
                                sumUpAchievement.HasSuffix = hasPostfix;
                            }
                            else
                            {
                                var standardAchievement = new StandardAchievement(clearOnce, secret, noHint, achievementId, goal, relatedRoles, types.ToArray(), rarity, attention, specifiedImage,preAchievement);
                                standardAchievement.HasInfix = hasInfix;
                                standardAchievement.HasPrefix = hasPrefix;
                                standardAchievement.HasSuffix = hasPostfix;
                            }
                            LoadedAchievementIds.Add(achievementId);
                            
                        }
                    }
                }

                LoadedAddons.Add(addon.Id);
                
            }
            catch (Exception ex)
            {
                
                // 失败时清除标记，允许下次重试
                LoadedAddons.Remove(addon.Id);
                // 回滚已加载的成就
                foreach (var id in LoadedAchievementIds.Where(x => x.StartsWith(addon.Id)).ToList())
                {
                    LoadedAchievementIds.Remove(id);
                }
            }
        }

        // 检查所有成就状态
        foreach (var achievement in NebulaAchievementManager.AllAchievements)
        {
            achievement.CheckClear();
        }

      
        yield break;
    }
}