using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckpointHSEWebServer
{
    public class EmployeesContext : DbContext //этот класс используется для работы с бд (всякие запросы, изменения)
    {
        public EmployeesContext(DbContextOptions<EmployeesContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; } //возвращает все поля модельки, куда подставляются значения из таблиц бд
    }
}
