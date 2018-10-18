using System;
using System.Collections.Generic;

namespace FileUploadDemo.Models
{
    public class FileIdsModel<T>
    {
        public IEnumerable<T> FileIds { get; set; }
    }
}
