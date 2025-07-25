﻿// ------------------------------------------------------------------------------
// <copyright file="MapInfo.cs" company="Drake53">
// Licensed under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;

using War3Net.Build.Common;
using War3Net.Build.Extensions;
using War3Net.Common.Extensions;

namespace War3Net.Build.Info
{
    public sealed partial class MapInfo
    {
        internal MapInfo(BinaryReader reader)
        {
            ReadFrom(reader);
        }

        internal void ReadFrom(BinaryReader reader)
        {
            FormatVersion = reader.ReadInt32<MapInfoFormatVersion>();
            if (FormatVersion >= MapInfoFormatVersion.v18)
            {
                MapVersion = reader.ReadInt32();
                EditorVersion = reader.ReadInt32<EditorVersion>();

                if (FormatVersion >= MapInfoFormatVersion.v27)
                {
                    GameVersion = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                }
            }

            MapName = reader.ReadChars();
            MapAuthor = reader.ReadChars();
            MapDescription = reader.ReadChars();
            RecommendedPlayers = reader.ReadChars();

            if (FormatVersion == MapInfoFormatVersion.v8)
            {
                Unk1 = reader.ReadSingle();
                Unk2 = reader.ReadInt32();
                Unk3 = reader.ReadSingle();
                Unk4 = reader.ReadSingle();
                Unk5 = reader.ReadSingle();
                Unk6 = reader.ReadInt32();
            }

            CameraBounds = reader.ReadQuadrilateral();
            if (FormatVersion >= MapInfoFormatVersion.v15)
            {
                CameraBoundsComplements = reader.ReadRectangleMargins();
            }

            PlayableMapAreaWidth = reader.ReadInt32();
            PlayableMapAreaHeight = reader.ReadInt32();

            if (FormatVersion == MapInfoFormatVersion.v8)
            {
                Unk7 = reader.ReadInt32();
            }

            MapFlags = reader.ReadInt32<MapFlags>();
            Tileset = reader.ReadByte<Tileset>();

            if (FormatVersion >= MapInfoFormatVersion.v23)
            {
                LoadingScreenBackgroundNumber = reader.ReadInt32();
                LoadingScreenPath = reader.ReadChars();
            }
            else if (FormatVersion >= MapInfoFormatVersion.v18)
            {
                CampaignBackgroundNumber = reader.ReadInt32();
            }
            else if (FormatVersion >= MapInfoFormatVersion.v15)
            {
                LoadingScreenPath = reader.ReadChars();
            }

            if (FormatVersion >= MapInfoFormatVersion.v10)
            {
                LoadingScreenText = reader.ReadChars();
                LoadingScreenTitle = reader.ReadChars();
                if (FormatVersion >= MapInfoFormatVersion.v15)
                {
                    LoadingScreenSubtitle = reader.ReadChars();
                }

                if (FormatVersion >= MapInfoFormatVersion.v23)
                {
                    GameDataSet = reader.ReadInt32<GameDataSet>();
                    PrologueScreenPath = reader.ReadChars();
                }
                else if (FormatVersion >= MapInfoFormatVersion.v18)
                {
                    LoadingScreenNumber = reader.ReadInt32();
                }
                else if (FormatVersion >= MapInfoFormatVersion.v15)
                {
                    PrologueScreenPath = reader.ReadChars();
                }

                if (FormatVersion >= MapInfoFormatVersion.v11)
                {
                    PrologueScreenText = reader.ReadChars();
                    PrologueScreenTitle = reader.ReadChars();
                    if (FormatVersion >= MapInfoFormatVersion.v15)
                    {
                        PrologueScreenSubtitle = reader.ReadChars();
                    }
                }

                if (FormatVersion >= MapInfoFormatVersion.v23)
                {
                    FogStyle = reader.ReadInt32<FogStyle>();
                    FogStartZ = reader.ReadSingle();
                    FogEndZ = reader.ReadSingle();
                    FogDensity = reader.ReadSingle();
                    FogColor = reader.ReadColorBgra();

                    if (FormatVersion >= MapInfoFormatVersion.v25)
                    {
                        GlobalWeather = reader.ReadInt32<WeatherType>();
                    }

                    SoundEnvironment = reader.ReadChars();
                    LightEnvironment = reader.ReadByte<Tileset>();
                    WaterTintingColor = reader.ReadColorBgra();
                }

                if (FormatVersion >= MapInfoFormatVersion.v28)
                {
                    ScriptLanguage = reader.ReadInt32<ScriptLanguage>();
                }

                if (FormatVersion >= MapInfoFormatVersion.v31)
                {
                    SupportedModes = reader.ReadInt32<SupportedModes>();
                    GameDataVersion = reader.ReadInt32<GameDataVersion>();
                }

                if (FormatVersion >= MapInfoFormatVersion.v32)
                {
                    ForceDefaultCameraZoom = reader.ReadInt32();
                    ForceMaxCameraZoom = reader.ReadInt32();
                }

                if (FormatVersion >= MapInfoFormatVersion.v33)
                {
                    ForceMinCameraZoom = reader.ReadInt32();
                }
            }

            nint playerDataCount = reader.ReadInt32();
            for (nint i = 0; i < playerDataCount; i++)
            {
                Players.Add(reader.ReadPlayerData(FormatVersion));
            }

            nint forceDataCount = reader.ReadInt32();
            for (nint i = 0; i < forceDataCount; i++)
            {
                Forces.Add(reader.ReadForceData(FormatVersion));
            }

            if (reader.ReadByte() == 255)
            {
                _skipData = true;
            }
            else
            {
                reader.BaseStream.Seek(-1, SeekOrigin.Current);

                nint upgradeDataCount = reader.ReadInt32();
                for (nint i = 0; i < upgradeDataCount; i++)
                {
                    UpgradeData.Add(reader.ReadUpgradeData(FormatVersion));
                }

                if (reader.BaseStream.Position == reader.BaseStream.Length)
                {
                    return;
                }

                nint techDataCount = reader.ReadInt32();
                for (nint i = 0; i < techDataCount; i++)
                {
                    TechData.Add(reader.ReadTechData(FormatVersion));
                }

                if (FormatVersion >= MapInfoFormatVersion.v15)
                {
                    nint randomUnitTableCount = reader.ReadInt32();
                    for (nint i = 0; i < randomUnitTableCount; i++)
                    {
                        RandomUnitTables!.Add(reader.ReadRandomUnitTable(FormatVersion));
                    }
                }

                if (FormatVersion >= MapInfoFormatVersion.v24)
                {
                    nint randomItemTableCount = reader.ReadInt32();
                    for (nint i = 0; i < randomItemTableCount; i++)
                    {
                        RandomItemTables!.Add(reader.ReadRandomItemTable(FormatVersion));
                    }
                }

                if (FormatVersion >= MapInfoFormatVersion.v26 && FormatVersion < MapInfoFormatVersion.v28)
                {
                    if (reader.ReadInt32() != 0)
                    {
                        throw new InvalidDataException();
                    }
                }
            }
        }

        internal void WriteTo(BinaryWriter writer)
        {
            if (CameraBounds is null)
            {
                throw new InvalidOperationException($"Cannot serialize {nameof(MapInfo)}, because {nameof(CameraBounds)} is null.");
            }

            writer.Write((int)FormatVersion);

            if (FormatVersion >= MapInfoFormatVersion.v18)
            {
                writer.Write(MapVersion);
                writer.Write((int)EditorVersion);

                if (FormatVersion >= MapInfoFormatVersion.v27)
                {
                    if (GameVersion is null)
                    {
                        throw new InvalidOperationException($"Cannot serialize {nameof(MapInfo)}, because {nameof(GameVersion)} is null.");
                    }

                    writer.Write(GameVersion.Major);
                    writer.Write(GameVersion.Minor);
                    writer.Write(GameVersion.Build);
                    writer.Write(GameVersion.Revision);
                }
            }

            writer.WriteString(MapName);
            writer.WriteString(MapAuthor);
            writer.WriteString(MapDescription);
            writer.WriteString(RecommendedPlayers);

            if (FormatVersion == MapInfoFormatVersion.v8)
            {
                writer.Write(Unk1);
                writer.Write(Unk2);
                writer.Write(Unk3);
                writer.Write(Unk4);
                writer.Write(Unk5);
                writer.Write(Unk6);
            }

            writer.Write(CameraBounds);
            if (FormatVersion >= MapInfoFormatVersion.v15)
            {
                if (CameraBoundsComplements is null)
                {
                    throw new InvalidOperationException($"Cannot serialize {nameof(MapInfo)}, because {nameof(CameraBoundsComplements)} is null.");
                }

                writer.Write(CameraBoundsComplements);
            }

            writer.Write(PlayableMapAreaWidth);
            writer.Write(PlayableMapAreaHeight);

            if (FormatVersion == MapInfoFormatVersion.v8)
            {
                writer.Write(Unk7);
            }

            writer.Write((int)MapFlags);
            writer.Write((byte)Tileset);

            if (FormatVersion >= MapInfoFormatVersion.v23)
            {
                writer.Write(LoadingScreenBackgroundNumber);
                writer.WriteString(LoadingScreenPath);
            }
            else if (FormatVersion >= MapInfoFormatVersion.v18)
            {
                writer.Write(CampaignBackgroundNumber);
            }
            else if (FormatVersion >= MapInfoFormatVersion.v15)
            {
                writer.WriteString(LoadingScreenPath);
            }

            if (FormatVersion >= MapInfoFormatVersion.v10)
            {
                writer.WriteString(LoadingScreenText);
                writer.WriteString(LoadingScreenTitle);
                if (FormatVersion >= MapInfoFormatVersion.v15)
                {
                    writer.WriteString(LoadingScreenSubtitle);
                }

                if (FormatVersion >= MapInfoFormatVersion.v23)
                {
                    writer.Write((int)GameDataSet);
                    writer.WriteString(PrologueScreenPath);
                }
                else if (FormatVersion >= MapInfoFormatVersion.v18)
                {
                    writer.Write(LoadingScreenNumber);
                }
                else if (FormatVersion >= MapInfoFormatVersion.v15)
                {
                    writer.WriteString(PrologueScreenPath);
                }

                if (FormatVersion >= MapInfoFormatVersion.v11)
                {
                    writer.WriteString(PrologueScreenText);
                    writer.WriteString(PrologueScreenTitle);
                    if (FormatVersion >= MapInfoFormatVersion.v15)
                    {
                        writer.WriteString(PrologueScreenSubtitle);
                    }
                }

                if (FormatVersion >= MapInfoFormatVersion.v23)
                {
                    writer.Write((int)FogStyle);
                    writer.Write(FogStartZ);
                    writer.Write(FogEndZ);
                    writer.Write(FogDensity);
                    writer.Write(FogColor.ToBgra());

                    if (FormatVersion >= MapInfoFormatVersion.v25)
                    {
                        writer.Write((int)GlobalWeather);
                    }

                    writer.WriteString(SoundEnvironment);
                    writer.Write((byte)LightEnvironment);

                    writer.Write(WaterTintingColor.ToBgra());
                }

                if (FormatVersion >= MapInfoFormatVersion.v28)
                {
                    writer.Write((int)ScriptLanguage);
                }

                if (FormatVersion >= MapInfoFormatVersion.v31)
                {
                    writer.Write((int)SupportedModes);
                    writer.Write((int)GameDataVersion);
                }

                if (FormatVersion >= MapInfoFormatVersion.v32)
                {
                    writer.Write(ForceDefaultCameraZoom);
                    writer.Write(ForceMaxCameraZoom);
                }

                if (FormatVersion >= MapInfoFormatVersion.v33)
                {
                    writer.Write(ForceMinCameraZoom);
                }
            }

            writer.Write(Players.Count);
            foreach (var player in Players)
            {
                writer.Write(player, FormatVersion);
            }

            writer.Write(Forces.Count);
            foreach (var force in Forces)
            {
                writer.Write(force, FormatVersion);
            }

            if (_skipData)
            {
                writer.Write((byte)255);
                return;
            }

            writer.Write(UpgradeData.Count);
            foreach (var upgrade in UpgradeData)
            {
                writer.Write(upgrade, FormatVersion);
            }

            writer.Write(TechData.Count);
            foreach (var tech in TechData)
            {
                writer.Write(tech, FormatVersion);
            }

            if (FormatVersion >= MapInfoFormatVersion.v15)
            {
                if (RandomUnitTables is null)
                {
                    throw new InvalidOperationException($"Cannot serialize {nameof(MapInfo)}, because {nameof(RandomUnitTables)} is null.");
                }

                writer.Write(RandomUnitTables.Count);
                foreach (var unitTable in RandomUnitTables)
                {
                    writer.Write(unitTable, FormatVersion);
                }
            }

            if (FormatVersion >= MapInfoFormatVersion.v24)
            {
                if (RandomItemTables is null)
                {
                    throw new InvalidOperationException($"Cannot serialize {nameof(MapInfo)}, because {nameof(RandomItemTables)} is null.");
                }

                writer.Write(RandomItemTables.Count);
                foreach (var itemTable in RandomItemTables)
                {
                    writer.Write(itemTable, FormatVersion);
                }
            }

            if (FormatVersion >= MapInfoFormatVersion.v26 && FormatVersion < MapInfoFormatVersion.v28)
            {
                writer.Write(0);
            }
        }
    }
}