﻿using System.Runtime.InteropServices;

namespace OpenVIII.Battle.Dat
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    public struct Abilities
    {
        #region Fields

        [FieldOffset(2)]
        public ushort AbilityID;

        [FieldOffset(1)]
        public byte Animation;

        /// <summary>
        /// Type of ability
        /// </summary>
        /// <remarks>0x2 magic, 0x4 item, 0x8 monsterAbility;</remarks>
        [FieldOffset(0)]
        public KernelFlag KernelID; 

        // ifrit states one of theses is an animation ID.
        private const string Unk = "Unknown";

        #endregion Fields

        #region Properties

        public ItemInMenu? Item =>
            (KernelID & KernelFlag.Item) != 0 && Memory.MItems != null && Memory.MItems.Items.Count > AbilityID
                ? Memory.MItems?.Items[AbilityID]
                : null;

        public Kernel.MagicData Magic =>
                    (KernelID & KernelFlag.Magic) != 0 && Memory.KernelBin.MagicData.Count > AbilityID
                ? Memory.KernelBin.MagicData[AbilityID]
                : null;

        public Kernel.EnemyAttacksData Monster =>
            (KernelID & KernelFlag.Monster) != 0 && Memory.KernelBin.EnemyAttacksData.Count > AbilityID
                ? Memory.KernelBin.EnemyAttacksData[AbilityID]
                : null;

        #endregion Properties

        #region Methods

        public override string ToString() => Magic?.Name ?? Monster?.Name ?? Item?.Name ?? Unk;

        #endregion Methods
    }
}