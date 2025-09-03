using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoAPI.Models;

namespace TodoAPITests.Models
{
    public class TasksResultDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<Tasks> Items { get; set; } = new List<Tasks>();
    }

}
