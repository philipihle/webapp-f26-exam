using Microsoft.EntityFrameworkCore;

namespace webapp_f26_exam.DAL;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }
}
