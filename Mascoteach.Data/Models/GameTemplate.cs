using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class GameTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string JsBundleUrl { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }

    public virtual ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();
}
