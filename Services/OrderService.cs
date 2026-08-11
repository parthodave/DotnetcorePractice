using Microsoft.EntityFrameworkCore;

namespace DotNet8WebAPI.Services
{
    public class OrderService
    {
        private readonly OurHeroDbContext _context;
        private readonly IServiceBusService _serviceBusService;

        public OrderService(
            OurHeroDbContext context,
            IServiceBusService serviceBusService)
        {
            _context = context;
            _serviceBusService = serviceBusService;
        }
    }
}
