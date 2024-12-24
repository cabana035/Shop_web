using System;
using System.Collections.Generic;

namespace Shop_web.Models.Db;

public partial class Banner
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Subtile { get; set; }

    public string? ImageName { get; set; }

    public short? Priority { get; set; }

    public string? Link { get; set; }

    public string? Position { get; set; }
}
