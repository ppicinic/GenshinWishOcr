using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GenshinWishOcr
{
    public class Selection : IEquatable<Selection>
    {
        public SelectionType Type { get; set; }
        public string Item { get; set; }
        public DateTime Date { get; set; }
        public int Index { get; set; }

        public Selection(int index, bool isWeapon, bool isCharacter, string v, DateTime date)
        {
            Index = index;
            if (isWeapon)
            {
                Type = SelectionType.Weapon;
            }
            if (isCharacter)
            {
                Type = SelectionType.Character;
            }
            Item = v;
            Date = date;
        }

        public bool Equals([AllowNull] Selection other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return Index == other.Index && Type == other.Type && Item.Equals(other.Item) && Date.Equals(other.Date);
            }
        }

        public override string ToString()
        {
            return $"{Index} {Type} {Item} {Date}";
        }
    }

    public enum SelectionType
    {
        Character,
        Weapon
    }
}
