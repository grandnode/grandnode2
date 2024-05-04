using AutoMapper;
using Grand.Infrastructure.Mapper;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;

namespace Widgets.Slider.Infrastructure.Mapper;

public class SliderMapperConfiguration : Profile, IAutoMapperProfile
{
    public SliderMapperConfiguration()
    {
        CreateMap<SlideModel, PictureSlider>()
            .ForMember(dest => dest.ObjectEntry, mo => mo.MapFrom(x => SetObjectEntry(x)))
            .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
            .ForMember(dest => dest.Locales, mo => mo.Ignore());

        CreateMap<PictureSlider, SlideModel>()
            .ForMember(dest => dest.CategoryId, mo => mo.MapFrom(x => GetCategoryId(x)))
            .ForMember(dest => dest.CollectionId, mo => mo.MapFrom(x => GetCollectionId(x)))
            .ForMember(dest => dest.BrandId, mo => mo.MapFrom(x => GetBrandId(x)))
            .ForMember(dest => dest.Locales, mo => mo.Ignore());

        CreateMap<SlideListModel, PictureSlider>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.Stores, mo => mo.Ignore());

        CreateMap<PictureSlider, SlideListModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.PictureUrl, mo => mo.Ignore());
    }

    public int Order => 0;

    private string SetObjectEntry(SlideModel model)
    {
        return model.SliderTypeId switch {
            (int)SliderType.HomePage => "",
            (int)SliderType.Category => model.CategoryId,
            (int)SliderType.Collection => model.CollectionId,
            (int)SliderType.Brand => model.BrandId,
            _ => ""
        };
    }

    private string GetCategoryId(PictureSlider pictureSlider)
    {
        return pictureSlider.SliderTypeId == SliderType.Category ? pictureSlider.ObjectEntry : "";
    }

    private string GetCollectionId(PictureSlider pictureSlider)
    {
        return pictureSlider.SliderTypeId == SliderType.Collection ? pictureSlider.ObjectEntry : "";
    }

    private string GetBrandId(PictureSlider pictureSlider)
    {
        return pictureSlider.SliderTypeId == SliderType.Brand ? pictureSlider.ObjectEntry : "";
    }
}