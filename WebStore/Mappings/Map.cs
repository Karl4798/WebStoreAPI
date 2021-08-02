using AutoMapper;
using WebStore.Models;
using WebStore.Models.DTOs;

namespace WebStore.Mappings
{
    public class Map : Profile
    {
        public Map()
        {
            CreateMap<Product, ProductDTO>().ReverseMap();
            CreateMap<Category, CategoryDTO>().ReverseMap();
        }
    }
}