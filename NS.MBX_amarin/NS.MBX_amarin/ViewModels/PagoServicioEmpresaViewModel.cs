﻿using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NS.MBX_amarin.ViewModels
{
	public class PagoServicioEmpresaViewModel : ViewModelBase
	{
        public PagoServicioEmpresaViewModel(INavigationService navigationService)
            : base(navigationService)
        {

        }

        public async Task NavegarCtaCargo()
        {
            await NavigationService.NavigateAsync("CtaCargo");
        }
    }
}