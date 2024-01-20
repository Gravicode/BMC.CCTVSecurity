using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BMC.CCTVMonitoring.Helpers;
public class ObjectContext : DbContext
{
    public DbSet<ObjectDetect> ObjectDetects { get; set; }
  

    public string DbPath { get; }

    public ObjectContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "objects.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

public class ObjectDetect
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long No { get; set; }
    public int CCTVNo { get; set; }
    public string Url { get; set; }
    public int ObjectCount { get; set; }
    public DateTime DetectedTime { get; set; }

}