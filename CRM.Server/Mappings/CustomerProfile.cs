using AutoMapper;
//using CRM.Server.Dtos.Customers;
using CRM.Server.DTOs;
using CRM.Server.Models;
using CRM.Server.Data;

namespace CRM.Server.Mapping
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customer, CustomerResponseDto>();
            CreateMap<CustomerCreateDto, Customer>();
            CreateMap<CustomerUpdateDto, Customer>();
        }
    }
}
