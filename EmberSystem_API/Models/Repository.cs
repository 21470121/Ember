using Microsoft.EntityFrameworkCore;

namespace ApplicationSecurity_Backend.Models
{
    public class Repository:IRepository
    {
        private readonly AppDbContext _appDbContext;


        public Repository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        //public async Task<Course[]> GetAllCoursesAsync()
        //{
        //    IQueryable<Course> query = _appDbContext.Courses;
        //    return await query.ToArrayAsync();
        //}
    }
}
