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