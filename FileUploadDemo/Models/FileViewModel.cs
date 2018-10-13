﻿using System;

namespace FileUploadDemo.Models
{
    public class FileViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public long size { get; set; }

        public DateTime CreatedDateUtc { get; set; }
    }
}
