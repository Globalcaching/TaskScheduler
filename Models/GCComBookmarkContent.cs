﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Models
{
    [PetaPoco.TableName("GCComBookmarkContent")]
    public class GCComBookmarkContent
    {
        public long GCComBookmarkListID { get; set; }
        public long GCComGeocacheID { get; set; }
    }
}
