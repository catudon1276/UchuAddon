using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Virial.Color;

namespace Hori.Core;

public static class Citations
{
    public static Citation SuperNewRoles { get; private set; } = new("supernewroles", NebulaAPI.AddonAsset.GetResource("from/SuperNewRoles.png")!.AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("SuperNewRoles")), null);
    public static Citation TownOfUs { get; private set; } = new("townofus", NebulaAPI.AddonAsset.GetResource("from/TownOfUs.png")!.AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("TownOfUs")), null);
    public static Citation TheOtherRoles { get; private set; } = new("theotherroles", NebulaAPI.AddonAsset.GetResource("from/TheOtherRoles.png")!.AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("TheOtherRoles")), null);
    public static Citation ExtremeRoles { get; private set; } = new("ExtremeRoles", NebulaAPI.AddonAsset.GetResource("from/ExtremeRoles.png")!.AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("Extreme Roles")), "https://github.com/yukieiji/ExtremeRoles");
    static public Citation TownOfHostY { get; private set; } = new("TownOfHostY", NebulaAPI.AddonAsset.GetResource("from/TownOfHost_Y.png")!.AsImage(150f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("Town Of Host Y")), "https://github.com/Yumenopai/TownOfHost_Y");
    static public Citation TownOfHostK { get; private set; } = new("TownOfHostK", NebulaAPI.AddonAsset.GetResource("from/TownOfHost-K.png")!.AsImage(150f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("Town Of Host K")), "https://github.com/KYMario/TownOfHost-K");
}
