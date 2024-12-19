using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoubleYou.Domain.Entities
{
    public sealed partial class Entities
    {
        public abstract class EntityBase
        {
            [NotMapped]
            private Guid _id = Guid.CreateVersion7();

            [Key]
            [Required]
            [Column("ID")]
            public Guid Id
            {
                get => _id;
                set
                {
                    if (value == Guid.Empty)
                    {
                        throw new ArgumentNullException(nameof(value), "Value is 'Guid.Empty'");
                    }

                    _id = value;
                }
            }
        }
    }
}