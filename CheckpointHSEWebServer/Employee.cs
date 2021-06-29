using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckpointHSEWebServer
{
    /// <summary>
    /// Моделька
    /// </summary>
    public class Employee
    {
        public string Name { get; set; }

        public string Surname { get; set; }

        public string Patronymic { get; set; }

        public string Guid { get; set; }

        public int Id { get; set; }
    }
}
