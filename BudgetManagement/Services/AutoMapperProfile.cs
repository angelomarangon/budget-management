using AutoMapper;
using BudgetManagement.Models;

namespace BudgetManagement.Services;

public class AutoMapperProfile: Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Cuenta, CuentaCreacionViewModel>();
        CreateMap<TransaccionActualizacionViewModel, Transaccion>().ReverseMap();
    }
}