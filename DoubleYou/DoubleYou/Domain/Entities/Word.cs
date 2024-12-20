/*  
    MIT License

    Copyright (c) 2024 Vlad Hirnyk (HERN1k)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DoubleYou.Domain.Enums;

namespace DoubleYou.Domain.Entities
{
    public sealed partial class Entities
    {
        public sealed class Word : EntityBase, IEquatable<Word>
        {
            [Required]
            [Column("Word")]
            public string Data { get; set; } = string.Empty;

            [Required]
            [Column("Topic")]
            public Topic Topic { get; set; } = Topic.Common;

            [Column("Learned_Date")]
            public DateTime? LearnedDate { get; set; } = null;

            public bool Equals(Word? other)
            {
                return other != null && Id == other.Id;
            }

            public override bool Equals(object? obj)
            {
                if (obj is Word otherDto)
                {
                    return Equals(otherDto);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }

            public override string ToString() => this.Data ?? string.Empty;
        }
    }
}