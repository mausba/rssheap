using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class OPML
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; }
        public bool Parsed { get; set; }
        public DateTime Date { get; set; }
    }
}
