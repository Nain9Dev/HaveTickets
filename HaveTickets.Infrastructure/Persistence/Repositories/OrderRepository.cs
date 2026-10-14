using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Ticketing;

namespace HaveTickets.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(HaveTicketsDbContext database) : IOrderRepository
{
    /// <summary>Tickets are reachable from the order, so EF inserts them in the same transaction.</summary>
    public void Add(Order order) => database.Orders.Add(order);
}
