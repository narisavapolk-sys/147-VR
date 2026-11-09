#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Applies the material + texture state that exists in the authoring .blend but never
    /// reached the Unity project.
    ///
    /// Where the numbers come from (2026-11-09, Unity closed for safety):
    ///   * 147VR_147NEON_VINTAGE_ArtPass_AAA naris.blend has 268 materials with a Principled
    ///     BSDF; 106 of them reference image textures, and 143 of those images were PACKED
    ///     INSIDE the .blend, i.e. they had never existed as files. They were written out to
    ///     Assets/147 main/BlendTextures/ (143 written, 0 failed).
    ///   * Each blend material was matched to its Unity asset by name with the repeated
    ///     "147VR_" prefixes stripped, giving 157 material targets: 131 with a diffuse image,
    ///     48 with a normal image, 33 with emission. Zero blend materials had no counterpart.
    ///   * Emission is converted the way Unity stores it: _EmissionColor = rgb x strength,
    ///     with alpha carrying the strength, so (1.0, 0.012, 0.035) at strength 4 becomes
    ///     (4, 0.048, 0.14, 4).
    ///
    /// Generated table format, one row per Unity material:
    ///   assetPath | diffuseTexture | normalTexture | emissionTexture | metallic | smoothness
    ///             | emissionRGBA | baseColorRGBA
    ///
    /// Menu 17 writes nothing and reports what it would change plus any missing file.
    /// Menu 18 applies through the Material API and SerializedObject (never hand-written YAML),
    /// sets the importer to NormalMap for normal textures, saves the assets, then RE-READS the
    /// saved .mat files from disk and prints PASS/FAIL per material. No Scene is touched.
    /// </summary>
    public static class WipRoomBlendMaterialSync
    {
        private const string DryRunReport = "Logs/WipRoom_BlendMaterialSync_DryRun.txt";
        private const string ApplyReport = "Logs/WipRoom_BlendMaterialSync_Apply.txt";

        private static readonly string[] Rows =
        {
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_147 VR Burgundy Red Velvet _ Sketchfab Curtains.mat|Assets/147 main/BlendTextures/material_2_transmission.png.png|Assets/147 main/BlendTextures/material_2_normal.png.png||0|0.73|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147 VR Burgundy Red Velvet _ Sketchfab Curtains.mat|Assets/147 main/BlendTextures/material_2_transmission.png.png|Assets/147 main/BlendTextures/material_2_normal.png.png||0|0.73|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR Stage _ Warm Marquee.mat||||0.25|0.75|2,0.54,0.11,2|0.65,0.26,0.045,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR _ Royal Gold.mat||||0.75|0.76|0.4,0.22,0.048,0.4|1,0.55,0.12,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR _ Ruby Neon.mat||||0.1|0.76|4,0.048,0.14,4|1,0.012,0.035,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR _ Warm Ivory.mat||||0.2|0.76|0.25,0.2075,0.13,0.25|1,0.83,0.52,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_Black_Gold_Classic_Damask_Inlay.mat|Assets/147 main/RoomTextures/gold_damask.jpg||Assets/147 main/RoomTextures/gold_damask.jpg|0.18|0.7|0.04,0.04,0.04,0.04|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_Dark_Mahogany_Antique_PBR.mat|Assets/147 main/RoomTextures/dark_wood_diff_4k.jpg|Assets/147 main/BlendTextures/dark_wood_nor_gl_2k.png.png||0|0.66|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_MAT_DrumHead_Logo.mat|Assets/147 main/BlendTextures/147VR_DrumHead_Logo_2048.png.001.png|||0|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_MAT_GamePlant_AlbedoPBR.mat|Assets/147 main/BlendTextures/0206_Plant Albedo.png.png|Assets/147 main/BlendTextures/0177_Plant Normal.png.png||0|0.32|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_MAT_RealCurtain_Gold.mat|Assets/147 main/BlendTextures/Gold lined_14.png.png|||0.55|0.65|0.15,0.117,0.06,0.15|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_MAT_RealCurtain_Velvet.mat|Assets/147 main/BlendTextures/red velvet_25.png.png|||0|0.28|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_AAA_MAT_Stage_Guitar_GameMesh_A.mat|Assets/147 main/BlendTextures/147VR_Texture_0046_guitar_png.png|Assets/147 main/BlendTextures/147VR_Texture_0055_guitar_N_png.png||0.04|0.68|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Aged_Brass_LoftLight.mat||||0.84|0.79|0.18,0.1224,0.0774,0.18|0.32,0.19,0.075,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_ArtDeco_Geometric_Gold_Brown.mat|Assets/147 main/BlendTextures/147VR_ArtDeco_Geometric_Gold_Brown_Texture.png|||0|0.16|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147vr_b3_poolSkin.mat|Assets/147 main/BlendTextures/0099_PoolTable_D.png.001.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Ceiling_Coffer.mat|Assets/147 main/BlendTextures/147VR_ceiling.png.png|Assets/147 main/BlendTextures/147VR_ceiling_nrm.png.png||0|0.38|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Coffer_Panel.mat|Assets/147 main/BlendTextures/0009_Wood_warm.png.png|||0|0.3|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_DJ_DiscoPanel.mat|Assets/147 main/RoomTextures/g99.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Faint_Air_Dust.mat||||0|0|0.0088,0.0048,0.0018,0.04|0.45,0.3,0.14,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_argentina.mat|Assets/147 main/BlendTextures/flag_argentina.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_austria.mat|Assets/147 main/BlendTextures/flag_austria.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_brazil.mat|Assets/147 main/BlendTextures/flag_brazil.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_canada.mat|Assets/147 main/BlendTextures/flag_canada.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_denmark.mat|Assets/147 main/BlendTextures/flag_denmark.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_finland.mat|Assets/147 main/BlendTextures/flag_finland.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_france.mat|Assets/147 main/BlendTextures/flag_france.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_germany.mat|Assets/147 main/BlendTextures/flag_germany.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_greece.mat|Assets/147 main/BlendTextures/flag_greece.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_india.mat|Assets/147 main/BlendTextures/flag_india.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_ireland.mat|Assets/147 main/BlendTextures/flag_ireland.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_italy.mat|Assets/147 main/BlendTextures/flag_italy.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_japan.mat|Assets/147 main/BlendTextures/flag_japan.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_netherlands.mat|Assets/147 main/BlendTextures/flag_netherlands.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_norway.mat|Assets/147 main/BlendTextures/flag_norway.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_poland.mat|Assets/147 main/BlendTextures/flag_poland.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_portugal.mat|Assets/147 main/BlendTextures/flag_portugal.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_russia.mat|Assets/147 main/BlendTextures/flag_russia.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_southafrica.mat|Assets/147 main/BlendTextures/flag_southafrica.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_spain.mat|Assets/147 main/BlendTextures/flag_spain.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_switzerland.mat|Assets/147 main/BlendTextures/flag_switzerland.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_thailand.mat|Assets/147 main/BlendTextures/flag_thailand.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_turkey.mat|Assets/147 main/BlendTextures/flag_turkey.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_uk.mat|Assets/147 main/BlendTextures/flag_uk.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_ukraine.mat|Assets/147 main/BlendTextures/flag_ukraine.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_FlagMat_usa.mat|Assets/147 main/BlendTextures/flag_usa.png.png|||0|0.4|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_French_Aubusson_Pastel_Floral.mat|Assets/147 main/BlendTextures/147VR_French_Aubusson_Pastel_Floral_Texture.png|||0|0.1|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_BarPultA.mat|Assets/147 main/BlendTextures/0191_T_BarPultA_BaseColor.png.png|||0.02|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_BarPultB.mat|Assets/147 main/BlendTextures/0139_T_BarPultB_BaseColor.png.001.png|||0.02|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_BCStoolsAndShelves.mat|Assets/147 main/BlendTextures/0151_T_BCStoolsAndShelves_BaseColor.png.001.png|||0.02|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_BeerTap.mat|Assets/147 main/BlendTextures/0195_T_BeerTap_1_BaseColor.png.001.png|||0|0.2828|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_BottleHolders.mat|Assets/147 main/BlendTextures/0173_T_BottleHolders_1_BaseColor.png.001.png|||0.05|0.72|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_CashDesk.mat|Assets/147 main/BlendTextures/0203_T_CashDesk_DefaultMaterial_BaseColor.png.001.png|||0|0.2828|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_Damask_Y.mat||Assets/147 main/BlendTextures/147VR_damask_normal.png.png||0|0.22|0,0,0,0|0.8,0.8,0.8,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_FloralValance.mat|Assets/147 main/BlendTextures/147VR_floral_valance_1792650903.png.png||Assets/147 main/BlendTextures/147VR_floral_valance_1792650903.png.png|0|0.4|0.35,0.35,0.35,0.35|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_GoldCarved.mat|Assets/147 main/BlendTextures/frieze_gold.png.png|||0.9|0.74|0.18,0.1044,0.036,0.18|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_GoldRail.mat||||1|0.885|0.14,0.077,0.0224,0.14|0.86,0.565,0.165,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_PannelFrame.mat|Assets/147 main/BlendTextures/0096_T_WoodPanel_Dark_BaseColor.png.png|||0.02|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_Sampagne.mat|Assets/147 main/BlendTextures/0187_T_Sampagne_1_BaseColor.png.png|||0.05|0.72|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_WelcomeGold.mat||||1|0.75|3,2.1,0.75,3|0.85,0.6,0.2,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_WhiskeyBottles.mat|Assets/147 main/BlendTextures/0194_T_WhiskeyBottles_1_BaseColor.png.001.png|||0.05|0.72|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Mat_WineBottles.mat|Assets/147 main/BlendTextures/0128_T_WineBottles_1_BaseColor.png.001.png|||0.05|0.72|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_MiraclePoolSkullTrophy.mat|Assets/147 main/BlendTextures/0065_SkullTrophy.png.png|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_00.mat|Assets/147 main/BlendTextures/147VR_poolart_A.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_01.mat|Assets/147 main/BlendTextures/147VR_poolart_B.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_02.mat|Assets/147 main/BlendTextures/147VR_poolart_C.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_03.mat|Assets/147 main/BlendTextures/147VR_poolart_A.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_04.mat|Assets/147 main/BlendTextures/147VR_poolart_B.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_05.mat|Assets/147 main/BlendTextures/147VR_poolart_C.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_06.mat|Assets/147 main/BlendTextures/147VR_poolart_A.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pic_07.mat|Assets/147 main/BlendTextures/147VR_poolart_B.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_MinusX_00_canvas.mat|Assets/147 main/RoomTextures/g00.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_MinusX_01_canvas.mat|Assets/147 main/RoomTextures/g01.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_MinusX_05_canvas.mat|Assets/147 main/RoomTextures/g02.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_PlusX_00_canvas.mat|Assets/147 main/RoomTextures/g03.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_PlusX_01_canvas.mat|Assets/147 main/RoomTextures/g04.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_PlusX_05_canvas.mat|Assets/147 main/RoomTextures/g05.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_PlusX_06_canvas.mat|Assets/147 main/RoomTextures/g06.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_PicNew_147VR_Gallery_PlusX_07_canvas.mat|Assets/147 main/RoomTextures/g07.jpg|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Pool_VR_Material.mat|Assets/147 main/BlendTextures/147VR_PoolTable_ALB.001.png|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Sparkle.mat||||0|0.5|9,8.28,6.75,9|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Turkish_Oushak_Vintage.mat|Assets/147 main/BlendTextures/147VR_Turkish_Oushak_Vintage_Texture.png|||0|0.12|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_Wall_Damask.mat|Assets/147 main/BlendTextures/147VR_damask_albedo.png.png|Assets/147 main/BlendTextures/147VR_damask_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_WallArt_New_01.mat|Assets/147 main/BlendTextures/art_snooker_overhead.png.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_WallArt_New_02.mat|Assets/147 main/BlendTextures/art_147_emblem.png.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/147VR_WallArt_New_03.mat|Assets/147 main/BlendTextures/art_stilllife.png.png|||0|0.45|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Chandelier_crystal.mat|Assets/147 main/RoomTextures/Diffuse.png|Assets/147 main/RoomTextures/nor_gl.png||0|0.5|0.0216,0.0108,0.003,0.12|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/Chandelier_crystal.mat|Assets/147 main/RoomTextures/Diffuse.png|Assets/147 main/RoomTextures/nor_gl.png||0|0.5|0.0216,0.0108,0.003,0.12|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Circle.010_Bake1_baked.002.mat|Assets/147 main/BlendTextures/Circle.010_Bake1_baked.002_baseColor.png.png|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/Circle.010_Bake1_baked.002.mat|Assets/147 main/BlendTextures/Circle.010_Bake1_baked.002_baseColor.png.png|||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_DefaultMaterial.mat|Assets/147 main/BlendTextures/DefaultMaterial_metallicRoughness.png.png|Assets/147 main/BlendTextures/DefaultMaterial_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/DefaultMaterial.mat|Assets/147 main/BlendTextures/DefaultMaterial_metallicRoughness.png.png|Assets/147 main/BlendTextures/DefaultMaterial_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/ImportedSnooker/Materials/V008/FELT.mat|Assets/147 main/BlendTextures/Green Felt Texture-2.png.png|||0|0.06|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_FELT.mat|Assets/147 main/BlendTextures/Green Felt Texture-2.png.png|||0|0.06|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/FELT.mat|Assets/147 main/BlendTextures/Green Felt Texture-2.png.png|||0|0.06|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Juke_Buttons.mat||||0.05|0.7|6,4.8,2.4,6|0.92,0.76,0.48,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/Juke_Buttons.mat||||0.05|0.7|6,4.8,2.4,6|0.92,0.76,0.48,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Juke_Cards.mat||||0|0.3|0.1,0.09,0.07,0.1|0.88,0.82,0.68,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/Juke_Cards.mat||||0|0.3|0.1,0.09,0.07,0.1|0.88,0.82,0.68,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Juke_NeonArch.mat||||0|0.5|12,3.36,0.48,12|1,0.35,0.05,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/Juke_NeonArch.mat||||0|0.5|12,3.36,0.48,12|1,0.35,0.05,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_lambert1.mat|Assets/147 main/BlendTextures/lambert1_metallicRoughness.png.png|Assets/147 main/BlendTextures/lambert1_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/lambert1.mat|Assets/147 main/BlendTextures/lambert1_metallicRoughness.png.png|Assets/147 main/BlendTextures/lambert1_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_lightbulb_01_base.mat|Assets/147 main/BlendTextures/lightbulb_01_diff.png.001.jpg|Assets/147 main/BlendTextures/lightbulb_01_nor_gl.png.001.exr|Assets/147 main/BlendTextures/lightbulb_01_emissive.png.png|0|0.5|10,7.2,4.2,10|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/lightbulb_01_base.mat|Assets/147 main/BlendTextures/lightbulb_01_diff.png.001.jpg|Assets/147 main/BlendTextures/lightbulb_01_nor_gl.png.001.exr|Assets/147 main/BlendTextures/lightbulb_01_emissive.png.png|0|0.5|10,7.2,4.2,10|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_lightbulb_01_glass.mat|Assets/147 main/BlendTextures/lightbulb_01_diff.png.jpg|Assets/147 main/BlendTextures/lightbulb_01_nor_gl.png.exr||0|0.76|1.4,1.022,0.602,1.4|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/lightbulb_01_glass.mat|Assets/147 main/BlendTextures/lightbulb_01_diff.png.jpg|Assets/147 main/BlendTextures/lightbulb_01_nor_gl.png.exr||0|0.76|1.4,1.022,0.602,1.4|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_M_DJMixer.mat|Assets/147 main/BlendTextures/M_DJMixer_metallicRoughness.png.png|Assets/147 main/BlendTextures/M_DJMixer_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/M_DJMixer.mat|Assets/147 main/BlendTextures/M_DJMixer_metallicRoughness.png.png|Assets/147 main/BlendTextures/M_DJMixer_normal.png.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_M_DrumKit_MainParts.001.mat|Assets/147 main/BlendTextures/M_DrumKit_MainParts.001_metallicRoughness.png.001.png|Assets/147 main/BlendTextures/M_DrumKit_MainParts.001_normal.png.001.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/M_DrumKit_MainParts.001.mat|Assets/147 main/BlendTextures/M_DrumKit_MainParts.001_metallicRoughness.png.001.png|Assets/147 main/BlendTextures/M_DrumKit_MainParts.001_normal.png.001.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_M_DrumKit_MetalBits.001.mat|Assets/147 main/BlendTextures/M_DrumKit_MetalBits.001_metallicRoughness.png.001.png|Assets/147 main/BlendTextures/M_DrumKit_MetalBits.001_normal.png.001.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/M_DrumKit_MetalBits.001.mat|Assets/147 main/BlendTextures/M_DrumKit_MetalBits.001_metallicRoughness.png.001.png|Assets/147 main/BlendTextures/M_DrumKit_MetalBits.001_normal.png.001.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_marble_01_material_SM_0011_Column_corner_NE.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/marble_01_material_SM_0011_Column_corner_NE.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_marble_01_material_SM_0011_Column_corner_NW.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/marble_01_material_SM_0011_Column_corner_NW.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_marble_01_material_SM_0011_Column_corner_SE.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/marble_01_material_SM_0011_Column_corner_SE.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_marble_01_material_SM_0011_Column_corner_SW.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/marble_01_material_SM_0011_Column_corner_SW.mat|Assets/147 main/BlendTextures/marble_01_Diffuse.jpg|Assets/147 main/BlendTextures/marble_01_nor_gl.jpg||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MAT_147VR_EspressoMachine_White.mat|Assets/147 main/BlendTextures/0198_T_EspressoMachine_1_OcclusionRoughnessMetallic.png.png|Assets/147 main/BlendTextures/0183_T_EspressoMachine_1_Normal.png.png||0.08|0.76|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MAT_147VR_EspressoMachine_White.mat|Assets/147 main/BlendTextures/0198_T_EspressoMachine_1_OcclusionRoughnessMetallic.png.png|Assets/147 main/BlendTextures/0183_T_EspressoMachine_1_Normal.png.png||0.08|0.76|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_material.mat|Assets/147 main/BlendTextures/material_metallicRoughness.png.png|Assets/147 main/BlendTextures/material_normal.png.png|Assets/147 main/RoomTextures/material_emissive.jpg|0|0.5|1,1,1,1|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/material.mat|Assets/147 main/BlendTextures/material_metallicRoughness.png.png|Assets/147 main/BlendTextures/material_normal.png.png|Assets/147 main/RoomTextures/material_emissive.jpg|0|0.5|1,1,1,1|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_material_1.mat|Assets/147 main/BlendTextures/material_1_metallicRoughness.png.png|Assets/147 main/BlendTextures/material_1_normal.png.png|Assets/147 main/RoomTextures/material_1_emissive.jpg|0|0.5|1,1,1,1|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/material_1.mat|Assets/147 main/BlendTextures/material_1_metallicRoughness.png.png|Assets/147 main/BlendTextures/material_1_normal.png.png|Assets/147 main/RoomTextures/material_1_emissive.jpg|0|0.5|1,1,1,1|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_maxelllogo.mat|Assets/147 main/BlendTextures/maxelllogo_baseColor.png.png|||0|0.1|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/maxelllogo.mat|Assets/147 main/BlendTextures/maxelllogo_baseColor.png.png|||0|0.1|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MP8_Carpet_8BP_A.mat|Assets/147 main/RoomTextures/Carpet_01_Base_Color 1.png|||0|0.05|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MP8_Carpet_8BP_A.mat|Assets/147 main/RoomTextures/Carpet_01_Base_Color 1.png|||0|0.05|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MP8_Carpet_8BP_B.mat|Assets/147 main/RoomTextures/Carpet_02_Base_Color.png|||0|0.05|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MP8_Carpet_8BP_B.mat|Assets/147 main/RoomTextures/Carpet_02_Base_Color.png|||0|0.05|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MP_Carpet.mat|Assets/AAA/Imports/NeonVintageArtPass/textures/0146_Carpet.png|||0|0.1|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MP_Carpet.mat|Assets/AAA/Imports/NeonVintageArtPass/textures/0146_Carpet.png|||0|0.1|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MV_0007_Leather.png.mat|Assets/147 main/BlendTextures/0007_Leather.png.png|||0|0.52|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MV_0007_Leather.png.mat|Assets/147 main/BlendTextures/0007_Leather.png.png|||0|0.52|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MV_0009_Wood_warm.png.mat|Assets/147 main/BlendTextures/0009_Wood_warm.png.png|||0|0.64|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MV_0009_Wood_warm.png.mat|Assets/147 main/BlendTextures/0009_Wood_warm.png.png|||0|0.64|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MV_0010_Wood_gray.png.mat|Assets/147 main/BlendTextures/0010_Wood_gray.png.png|||0|0.3|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MV_0010_Wood_gray.png.mat|Assets/147 main/BlendTextures/0010_Wood_gray.png.png|||0|0.3|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MV_0012_Metal_antique.png.mat|Assets/147 main/BlendTextures/0012_Metal_antique.png.png|||0.62|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MV_0012_Metal_antique.png.mat|Assets/147 main/BlendTextures/0012_Metal_antique.png.png|||0.62|0.62|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_MV_emission.mat||||0|0.62|1.5,0.57,0.18,1.5|0.35,0.16,0.045,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/MV_emission.mat||||0|0.62|1.5,0.57,0.18,1.5|0.35,0.16,0.045,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_PBR_floor.mat||Assets/147 main/RoomTextures/herringbone_parquet_nor_gl.png||0|0.5|0,0,0,0|0.8,0.8,0.8,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/PBR_floor.mat||Assets/147 main/RoomTextures/herringbone_parquet_nor_gl.png||0|0.5|0,0,0,0|0.8,0.8,0.8,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_PBR_floor.001.mat||Assets/147 main/BlendTextures/herringbone_parquet_nor_gl.jpg.001.jpg||0|0.5|0,0,0,0|0.8,0.8,0.8,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/PBR_floor.001.mat||Assets/147 main/BlendTextures/herringbone_parquet_nor_gl.jpg.001.jpg||0|0.5|0,0,0,0|0.8,0.8,0.8,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_PBR_wall.mat|Assets/147 main/RoomTextures/dark_paneled_wood_diff.png|Assets/147 main/RoomTextures/dark_paneled_wood_nor_gl.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/PBR_wall.mat|Assets/147 main/RoomTextures/dark_paneled_wood_diff.png|Assets/147 main/RoomTextures/dark_paneled_wood_nor_gl.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_PBR_wall_WS.mat|Assets/147 main/RoomTextures/dark_paneled_wood_diff.png|Assets/147 main/RoomTextures/dark_paneled_wood_nor_gl.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/PBR_wall_WS.mat|Assets/147 main/RoomTextures/dark_paneled_wood_diff.png|Assets/147 main/RoomTextures/dark_paneled_wood_nor_gl.png||0|0.5|0,0,0,0|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_TableSurface.mat||Assets/147 main/BlendTextures/SurfaceNormal_1024x1024.png.png||0|0|0,0,0,0|1,0.651,0.1627,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/TableSurface.mat||Assets/147 main/BlendTextures/SurfaceNormal_1024x1024.png.png||0|0|0,0,0,0|1,0.651,0.1627,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_TC_cove.mat||||0|0|0.75,0.54,0.315,0.75|1,1,1,1",
            "Assets/AAA/Imports/NeonVintageArtPass/Materials/TC_cove.mat||||0|0|0.75,0.54,0.315,0.75|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_147VR_MAT_Neon_Ruby_Hot.mat||||0.1|0.76|4,0.048,0.14,4|1,0.012,0.035,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_147VR_MAT_Neon_Ivory_Hot.mat||||0.2|0.76|0.25,0.2075,0.13,0.25|1,0.83,0.52,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Table Mahogany.mat|Assets/147 main/RoomTextures/Wood_Mahogany_Diffuse_mqt_2048.png|Assets/147 main/RoomTextures/Wood_Mahogany_Normal_mqt_2048.png||0|0.73|0,0,0,1|1,1,1,1",
            "Assets/AAA/Imports/naris_vintage/Rebound/147VR_Table Mahogany.001.mat|Assets/147 main/RoomTextures/Wood_Mahogany_Diffuse_mqt_2048.png|Assets/147 main/RoomTextures/Wood_Mahogany_Normal_mqt_2048.png||0|0.7|0,0,0,1|1,1,1,1",
        };

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/17) Sync Materials From Blend - Dry Run (Read-Only)")]
        public static void DryRun() => Run(false);

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/18) Sync Materials From Blend - Apply (Assets Only / No Scene Save)")]
        public static void Apply() => Run(true);

        private sealed class Row
        {
            public string Mat, Diffuse, Normal, Emit;
            public float Metallic = float.NaN, Smooth = float.NaN;
            public float[] Emission, Tint;
        }

        private static List<Row> Parse()
        {
            var list = new List<Row>();
            foreach (string raw in Rows)
            {
                string[] f = raw.Split('|');
                if (f.Length < 8) continue;
                var r = new Row { Mat = f[0], Diffuse = f[1], Normal = f[2], Emit = f[3] };
                if (!string.IsNullOrEmpty(f[4])) r.Metallic = float.Parse(f[4], System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(f[5])) r.Smooth = float.Parse(f[5], System.Globalization.CultureInfo.InvariantCulture);
                r.Emission = ParseFloats(f[6]);
                r.Tint = ParseFloats(f[7]);
                list.Add(r);
            }
            return list;
        }

        private static float[] ParseFloats(string s)
        {
            string[] p = (s ?? string.Empty).Split(',');
            var v = new float[4];
            for (int i = 0; i < 4 && i < p.Length; i++)
                float.TryParse(p[i], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v[i]);
            return v;
        }

        private static void Run(bool apply)
        {
            List<Row> rows = Parse();
            var sb = new StringBuilder();
            sb.AppendLine(apply ? "147VR BLEND MATERIAL SYNC - APPLY" : "147VR BLEND MATERIAL SYNC - DRY RUN (read-only)");
            sb.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Rows: " + rows.Count + " | source: 147VR_147NEON_VINTAGE_ArtPass_AAA naris.blend (268 materials, 143 packed images extracted)");
            sb.AppendLine("Scope: material assets + texture importers only. No Scene, Prefab or mesh is touched.");
            sb.AppendLine();

            int wouldChange = 0, alreadyOk = 0, missingMat = 0, missingTex = 0, normalImporters = 0;
            var touched = new List<Material>();
            var normalTextures = new List<string>();

            foreach (Row r in rows.OrderBy(x => x.Mat))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(r.Mat);
                if (m == null) { missingMat++; sb.AppendLine("  MISSING MATERIAL " + r.Mat); continue; }

                Texture2D diff = string.IsNullOrEmpty(r.Diffuse) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(r.Diffuse);
                Texture2D nrm = string.IsNullOrEmpty(r.Normal) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(r.Normal);
                Texture2D emis = string.IsNullOrEmpty(r.Emit) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(r.Emit);
                if (!string.IsNullOrEmpty(r.Diffuse) && diff == null) { missingTex++; sb.AppendLine("  MISSING TEXTURE " + r.Diffuse + " (for " + Path.GetFileName(r.Mat) + ")"); }
                if (!string.IsNullOrEmpty(r.Normal) && nrm == null) { missingTex++; sb.AppendLine("  MISSING TEXTURE " + r.Normal + " (for " + Path.GetFileName(r.Mat) + ")"); }

                bool needsDiffuse = diff != null && m.GetTexture("_BaseMap") != diff;
                bool needsNormal = nrm != null && m.GetTexture("_BumpMap") != nrm;
                bool needsEmit = r.Emission[3] > 0.011f && ColourChanged(m, r.Emission);
                bool needsTint = r.Tint != null && r.Tint.Length >= 3 && TintChanged(m, r.Tint);
                bool needsValues = (!float.IsNaN(r.Metallic) && Mathf.Abs(m.GetFloat("_Metallic") - r.Metallic) > 0.01f) ||
                                   (!float.IsNaN(r.Smooth) && Mathf.Abs(m.GetFloat("_Smoothness") - r.Smooth) > 0.01f);

                if (!needsDiffuse && !needsNormal && !needsEmit && !needsTint && !needsValues)
                {
                    alreadyOk++;
                    continue;
                }

                wouldChange++;
                sb.AppendLine("  " + (apply ? "SYNC " : "WOULD SYNC ") + r.Mat.Replace("Assets/AAA/Imports/", "").Replace("Assets/", "") +
                              (needsDiffuse ? "  baseMap=" + Path.GetFileName(r.Diffuse) : "") +
                              (needsNormal ? "  bump=" + Path.GetFileName(r.Normal) : "") +
                              (needsEmit ? "  emission=(" + string.Join(",", r.Emission.Select(v => v.ToString("F3"))) + ")" : "") +
                              (needsTint ? "  tint=(" + string.Join(",", (r.Tint ?? new float[0]).Select(v => v.ToString("F3"))) + ")" : "") +
                              (needsValues ? "  metal=" + r.Metallic + " smooth=" + r.Smooth : ""));

                if (!apply)
                {
                    if (nrm != null) normalTextures.Add(r.Normal);
                    continue;
                }

                Undo.RecordObject(m, "147VR Blend Material Sync");
                if (diff != null) m.SetTexture("_BaseMap", diff);
                if (nrm != null) m.SetTexture("_BumpMap", nrm);
                if (emis != null) m.SetTexture("_EmissionMap", emis);
                if (r.Tint != null && r.Tint.Length >= 3 && m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", new Color(r.Tint[0], r.Tint[1], r.Tint[2], r.Tint.Length > 3 ? r.Tint[3] : 1f));
                if (!float.IsNaN(r.Metallic) && m.HasProperty("_Metallic")) m.SetFloat("_Metallic", r.Metallic);
                if (!float.IsNaN(r.Smooth) && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", r.Smooth);
                if (r.Emission[3] > 0.011f && m.HasProperty("_EmissionColor"))
                    m.SetColor("_EmissionColor", new Color(r.Emission[0], r.Emission[1], r.Emission[2], r.Emission[3]));

                var wanted = new List<string>();
                if (nrm != null) wanted.Add("_NORMALMAP");
                if (r.Emission[3] > 0.011f) wanted.Add("_EMISSION");
                WriteKeywords(m, wanted);

                MaterialGlobalIlluminationFlags gi = m.globalIlluminationFlags;
                if (r.Emission[3] > 0.011f)
                {
                    gi &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                    gi |= MaterialGlobalIlluminationFlags.BakedEmissive;
                    m.globalIlluminationFlags = gi;
                }
                EditorUtility.SetDirty(m);
                touched.Add(m);
                if (nrm != null) { normalTextures.Add(r.Normal); normalImporters++; }
            }

            if (apply && normalTextures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("NORMAL-MAP IMPORTER SETTINGS (a normal map imported as a colour map shades wrong)");
                foreach (string texPath in normalTextures.Distinct())
                {
                    var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                    if (importer == null) { sb.AppendLine("  no importer for " + texPath); continue; }
                    if (importer.textureType == TextureImporterType.NormalMap) continue;
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.SaveAndReimport();
                    sb.AppendLine("  set NormalMap + non-sRGB: " + Path.GetFileName(texPath));
                }
            }

            if (apply && touched.Count > 0) AssetDatabase.SaveAssets();

            if (apply)
            {
                sb.AppendLine();
                sb.AppendLine("SELF-CHECK (re-read from disk, not from memory)");
                int failed = 0;
                foreach (Row r in rows.OrderBy(x => x.Mat))
                {
                    if (!File.Exists(r.Mat)) continue;
                    string txt = File.ReadAllText(r.Mat);
                    var problems = new List<string>();
                    if (!string.IsNullOrEmpty(r.Diffuse))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(r.Diffuse);
                        if (!Regex.IsMatch(txt, "_BaseMap:\\s*\\n\\s*m_Texture: \\{fileID: 2800000, guid: " + guid))
                            problems.Add("baseMap not on disk");
                    }
                    if (!string.IsNullOrEmpty(r.Normal))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(r.Normal);
                        if (!Regex.IsMatch(txt, "_BumpMap:\\s*\\n\\s*m_Texture: \\{fileID: 2800000, guid: " + guid))
                            problems.Add("bumpMap not on disk");
                        if (!txt.Contains("- _NORMALMAP")) problems.Add("_NORMALMAP keyword missing");
                    }
                    if (r.Emission[3] > 0.011f && !txt.Contains("- _EMISSION")) problems.Add("_EMISSION keyword missing");
                    if (problems.Count > 0)
                    {
                        failed++;
                        sb.AppendLine("  FAIL " + Path.GetFileName(r.Mat) + ": " + string.Join("; ", problems));
                    }
                }
                sb.AppendLine("  self-check failures: " + failed);
                sb.AppendLine();
                sb.AppendLine("SUMMARY rows=" + rows.Count + " changed=" + wouldChange + " alreadyCorrect=" + alreadyOk +
                              " missingMaterials=" + missingMat + " missingTextures=" + missingTex +
                              " normalMapsTagged=" + normalImporters);
                sb.AppendLine("Scene was NOT saved and NOT modified.");
                Write(apply ? ApplyReport : DryRunReport, sb);
                EditorUtility.DisplayDialog("147VR Blend Sync",
                    "Applied " + wouldChange + " materials (" + alreadyOk + " already matched)." +
                    "\nMissing materials: " + missingMat + " | missing textures: " + missingTex +
                    "\nNormal maps tagged: " + normalImporters +
                    "\n\nReport: " + ApplyReport, "OK");
                return;
            }

            sb.AppendLine();
            sb.AppendLine("SUMMARY rows=" + rows.Count + " wouldChange=" + wouldChange + " alreadyCorrect=" + alreadyOk +
                          " missingMaterials=" + missingMat + " missingTextures=" + missingTex);
            sb.AppendLine("Normal textures that will also be tagged as NormalMap by the importer: " + normalTextures.Distinct().Count());
            Write(DryRunReport, sb);
            Debug.Log("[BlendSync] dry-run rows=" + rows.Count + " wouldChange=" + wouldChange + " -> " + DryRunReport);
            EditorUtility.DisplayDialog("147VR Blend Sync",
                "Dry run complete. Nothing was modified.\n\n" + DryRunReport +
                "\nRows: " + rows.Count + "\nWould change: " + wouldChange +
                "\nAlready matching the blend: " + alreadyOk +
                "\nMissing textures: " + missingTex, "OK");
        }

        /// <summary>Material.EnableKeyword() does not persist into an asset .mat file, so the
        /// m_ValidKeywords array is written through SerializedObject and verified from disk.</summary>
        private static void WriteKeywords(Material m, List<string> wanted)
        {
            if (wanted.Count == 0) return;
            var so = new SerializedObject(m);
            SerializedProperty list = so.FindProperty("m_ValidKeywords");
            if (list == null || !list.isArray) return;
            foreach (string keyword in wanted)
            {
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (string.Equals(list.GetArrayElementAtIndex(i).stringValue, keyword, StringComparison.Ordinal)) { present = true; break; }
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = keyword;
            }
            so.ApplyModifiedProperties();
        }

        private static bool ColourChanged(Material m, float[] want)
        {
            if (!m.HasProperty("_EmissionColor")) return false;
            Color c = m.GetColor("_EmissionColor");
            return Mathf.Abs(c.r - want[0]) > 0.01f || Mathf.Abs(c.g - want[1]) > 0.01f ||
                   Mathf.Abs(c.b - want[2]) > 0.01f || Mathf.Abs(c.a - want[3]) > 0.01f;
        }

        private static bool TintChanged(Material m, float[] want)
        {
            if (!m.HasProperty("_BaseColor") || want == null || want.Length < 3) return false;
            Color c = m.GetColor("_BaseColor");
            return Mathf.Abs(c.r - want[0]) > 0.01f || Mathf.Abs(c.g - want[1]) > 0.01f || Mathf.Abs(c.b - want[2]) > 0.01f;
        }

        private static void Write(string p, StringBuilder sb)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(p, sb.ToString());
            Debug.Log("[BlendSync] " + p);
        }
    }
}
#endif
