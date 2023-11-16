﻿using Squad.Bot.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Squad.Bot.Models.AI
{
    [Table("joinDate")]
    public class JoinDate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("userId")]
        public Users User { get; set; } = null!;

        [Required]
        [Column("serverId")]
        public Guilds Guilds { get; set; } = null!;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
