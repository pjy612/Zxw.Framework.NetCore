using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Zxw.Framework.NetCore.DbContextCore;
using Zxw.Framework.NetCore.Models;

namespace Zxw.Framework.UnitTest.TestModels
{
    [Serializable]
    [DbContext(typeof(InMemoryDbContext))]
    [Table("TodoItems")]
    public class TodoItem : BaseModel<int>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public override int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        public int Priority { get; set; }

        public bool IsDone { get; set; }
    }
}
