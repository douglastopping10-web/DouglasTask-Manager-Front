using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_manager
{
    public class CreatedTaskResponse
    {
        public int Id { get; set; }
    }
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public int Version { get; set; } = 0;
    }
}
