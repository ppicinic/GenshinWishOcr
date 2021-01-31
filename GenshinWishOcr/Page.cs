using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GenshinWishOcr
{
    class MyPage : IEquatable<MyPage>
    {
        public List<Selection> Selections { get; set; }
        public int Index { get; set; } = -1;
        public MyPage()
        {
            Selections = new List<Selection>();
        }

        public bool Equals([AllowNull] MyPage other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (Selections.Count != other.Selections.Count)
                {
                    return false;
                }
                for (int i = 0; i < Selections.Count; i++)
                {
                    if (!Selections[i].Equals(other.Selections[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
