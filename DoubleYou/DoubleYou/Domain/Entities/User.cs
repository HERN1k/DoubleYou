using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DoubleYou.Domain.Enums;

namespace DoubleYou.Domain.Entities
{
    public sealed partial class Entities
    {
        public sealed class User : EntityBase, IEquatable<User>
        {
            [Required]
            [Column("Culture_Code")]
            public string CultureCode { get; set; } = "en-US";

            [Required]
            [Column("Translation_Language")]
            public string TranslationLanguage { get; set; } = string.Empty;

            [Required]
            [Column("Favorite_Topic")]
            public Topic FavoriteTopic { get; set; } = Topic.Programming;

            [Required]
            [Column("Is_Dialog_Show_Install_Voice")]
            public bool IsDialogShowInstallVoice { get; set; } = false;

            [Required]
            [Column("Created_Utc")]
            public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

            public bool Equals(User? other)
            {
                return other != null && Id == other.Id;
            }

            public override bool Equals(object? obj)
            {
                if (obj is User otherDto)
                {
                    return Equals(otherDto);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }

            public override string ToString()
            {
                return string.Concat("ID: ", Id.ToString());
            }
        }
    }
}