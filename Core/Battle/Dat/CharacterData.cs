﻿using Microsoft.Xna.Framework;

namespace OpenVIII.Battle.Dat
{
    public struct CharacterData
    {
        #region Fields

        public DatFile Character, Weapon;

        #endregion Fields

        #region Properties

        public Vector3 Location { get; internal set; }

        #endregion Properties
    };
}