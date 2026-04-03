using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Line_bot.Models;

public partial class Check
{
    [Key] // 代表這是 Primary Key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Lineuserid { get; set; }

    public DateTime? Checktime { get; set; }

    public string? Category { get; set; }

    public string? Address { get; set; }

    public decimal? Distance { get; set; }

    public bool? Isvalid { get; set; }
}
