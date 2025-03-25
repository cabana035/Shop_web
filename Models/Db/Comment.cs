using System;
using System.Collections.Generic;

namespace Shop_web.Models.Db;

public partial class Comment
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string CommentText { get; set; } = null!;

    public int ProductId { get; set; }

    public DateTime CreateDate { get; set; }

    public string? Name { get; set; }
}
