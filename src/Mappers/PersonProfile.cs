using AutoMapper;

namespace Hearth.ArcGIS.Samples.Mappers
{
    public class PersonProfile : Profile
    {
        public PersonProfile()
        {
            CreateMap<Person, PersonVO>();
            CreateMap<PersonVO, Person>();
        }
    }
}
