﻿using System;
using System.Collections.Generic;

namespace Shop_web.Models.Db;

public partial class Setting
{
    public int Id { get; set; }

    public decimal? Shipping { get; set; }
}
