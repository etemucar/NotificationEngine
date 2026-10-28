using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationEngine.Core.Base
{
    public abstract class DtoBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual long Id { get; set; }
        public virtual int StatusId { get; set; }
        public virtual int IsDeleted { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]

        public virtual DateTime? CreateDate { get; set; }

        public virtual int? CreatedBy { get; set; }
    }
}
