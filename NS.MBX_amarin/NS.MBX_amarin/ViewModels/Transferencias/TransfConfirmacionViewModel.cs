﻿using NS.MBX_amarin.BusinessLogic.Transacciones;
using NS.MBX_amarin.Common;
using NS.MBX_amarin.Events;
using NS.MBX_amarin.Model;
using NS.MBX_amarin.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NS.MBX_amarin.ViewModels
{
    public class TransfConfirmacionViewModel : ViewModelBase
    {
        private ICatalogoService CatalogoService { get; set; }
        private ICuentaService CuentaService { get; set; }
        private IOperacionService OperacionService { get; set; }
        private IEventAggregator EventAggregator { get; set; }
        
        public TransfConfirmacionViewModel(IOperacionService operacionService, ICuentaService cuentaService, ICatalogoService catalogoService, INavigationService navigationService, IPageDialogService dialogService, IEventAggregator eventAggregator)
            : base(navigationService, dialogService)
        {
            this.CatalogoService = catalogoService;
            this.CuentaService = cuentaService;
            this.EventAggregator = eventAggregator;
            this.OperacionService = operacionService;
        }

        public override void OnNavigatingTo(NavigationParameters parametros)
        {
            RefNavParameters = parametros;

            string pageOrigen = parametros[Constantes.pageOrigen] as string;
            Cuenta ctaOrigen = parametros["CtaCargo"] as Cuenta;
            Cuenta ctaDestino = parametros["CtaDestino"] as Cuenta;
            LblNombreCta1 = ctaOrigen.NombreCta;
            LblCodCta1 = ctaOrigen.CodigoCta;

            if (pageOrigen == Constantes.pageTransfCtaPropiaDatos)
            {
                LblNombreCta2 = ctaDestino.NombreCta;
                LblCodCta2 = ctaDestino.CodigoCta;
                LblTipoOpe = "Transferencia";
            }else if(pageOrigen == Constantes.pageTransfCtaTerceroDestino)
            {
                LblNombreCta2 = ctaDestino.NombreTitular;
                LblCodCta2 = ctaDestino.CodigoCta;
                LblTipoOpe = "Transferencia";
            }
            
        }

        private string _nomOpeFrec;
        public string NomOpeFrec
        {
            get { return _nomOpeFrec; }
            set { SetProperty(ref _nomOpeFrec, value); }
        }

        private string _lblNombreCta1;
        public string LblNombreCta1
        {
            get { return _lblNombreCta1; }
            set { SetProperty(ref _lblNombreCta1, value); }
        }

        private string _lblCodCta1;
        public string LblCodCta1
        {
            get { return _lblCodCta1; }
            set { SetProperty(ref _lblCodCta1, value); }
        }

        private string _lblNombreCta2;
        public string LblNombreCta2
        {
            get { return _lblNombreCta2; }
            set { SetProperty(ref _lblNombreCta2, value); }
        }

        private string _lblCodCta2;
        public string LblCodCta2
        {
            get { return _lblCodCta2; }
            set { SetProperty(ref _lblCodCta2, value); }
        }

        private string _claveSms;
        public string ClaveSms
        {
            get { return _claveSms; }
            set { SetProperty(ref _claveSms, value); }
        }

        private bool _isOperacionFrecuente;
        public bool IsOperacionFrecuente
        {
            get { return _isOperacionFrecuente; }
            set { SetProperty(ref _isOperacionFrecuente, value); }
        }

        private string _lblTipoOpe;
        public string LblTipoOpe
        {
            get { return _lblTipoOpe; }
            set { SetProperty(ref _lblTipoOpe, value); }
        }

        private DelegateCommand _accionConfirmarIC;
        public DelegateCommand AccionConfirmarIC =>
            _accionConfirmarIC ?? (_accionConfirmarIC = new DelegateCommand(ExecuteAccionConfirmarIC));

        async void ExecuteAccionConfirmarIC()
        {
            try
            {
                string msj = ValidarCampos();
                if (msj != "")
                {
                    await DialogService.DisplayAlertAsync("Validación", msj, "OK");
                    return;
                }

                Operacion operacion = RefNavParameters["Operacion"] as Operacion;
                SubOperacion suboperacion = RefNavParameters["SubOperacion"] as SubOperacion;

                //parametros
                if (operacion.Id == "3")
                {
                    if (suboperacion.Id == "2")
                    {
                        await AccionCtaPropia();
                    }
                    else if (suboperacion.Id == "0")
                    {
                        await AccionCtaTercero();
                    }
                }
                   
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        public string ValidarCampos()
        {
            string msj = "";

            if (string.IsNullOrEmpty(ClaveSms))
            {
                msj = "Ingrese una clave válida.";
            }else if(IsOperacionFrecuente && string.IsNullOrEmpty(NomOpeFrec))
            {
                msj = "Ingrese un nombre para la operación frecuente.";
            }

            return msj;
        }

        public async Task AccionCtaPropia()
        {
            NavigationParameters parametros = GetNavigationParameters();
            parametros.Add(Constantes.pageOrigen, Constantes.pageTransfConfirmacion);

            Operacion operacion = parametros["Operacion"] as Operacion;
            SubOperacion suboperacion = parametros["SubOperacion"] as SubOperacion;

            string monto = parametros["Monto"] as string;
            Catalogo moneda = parametros["Moneda"] as Catalogo;
            Cuenta ctaOrigen = parametros["CtaCargo"] as Cuenta;
            Cuenta ctaDestino = parametros["CtaDestino"] as Cuenta;

            decimal montoDec = decimal.Parse(monto);
            string codOperacionGenerado = "";
            DateTime fechaOperacion;

            //seccion IBS
            string _strError = "";
            double dblMontoReal = System.Math.Round(double.Parse(monto), 2);
            string strMontoReal = Convert.ToString(System.Math.Round(dblMontoReal, 2) * 100).PadLeft(14, '0');

            string tlog = DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0');

            string strMensaje = '%' + ctaOrigen.NombreCta.PadLeft(12, '0') + '%' + tlog + '%' + monto.PadLeft(14, '0') + '%' + ctaDestino.NombreCta.PadLeft(12, '0') + '%' + moneda.Codigo + "".PadRight(30, ' ') + '0' + "".PadLeft(48, ' ') + strMontoReal;

            Transacciones tx = new Transacciones();
            DataSet dsOut = tx.EjecutarTransaccion(ListaTransacciones.TrasferenciaEjecuta, 217, strMensaje, ListaTransacciones.NombreMensajeOut(), ListaTransacciones.PosicionInicialCorte(), out _strError);
            DataSet dsHeader = tx.ObtenerCabecera(ListaTransacciones.TrasferenciaEjecuta, ListaTransacciones.NombreMensajeOut(), 0);
            if (_strError == "0000")
            {
                if (dsOut != null && dsHeader != null)
                {
                    //base.InsertaLogTransferencias(TipoTransaccion.Transferencias(), TipoOperacion.Transaccion(), DefaultValues.TrasferenciaEjecuta, strMensaje, dsOut, _strError, dsHeader.Tables["OHead"].Rows[0]["OHlogtx"].ToString(), ddlCuentaOrigen.SelectedValue.Substring(4, 12), ddlCuentaOrigen.SelectedItem.Text + '|' + dsOut.Tables["OData"].Rows[0]["ODclifr"].ToString(), dsOut.Tables["OData"].Rows[0]["ODmtoor"].ToString(), strMonedaOrigen, ddlCuentaDestino.SelectedValue.Substring(4, 12), " | " + ddlCuentaDestino.SelectedItem.Text + " | " + dsOut.Tables["OData"].Rows[0]["ODclito"].ToString(), dsOut.Tables["OData"].Rows[0]["ODmtocv"].ToString(), strMonedaDestino, string.Empty, string.Empty, dsOut.Tables["OData"].Rows[0]["ODticam"].ToString(), dsOut.Tables["OData"].Rows[0]["ODItf"].ToString(), dsOut.Tables["OData"].Rows[0]["ODComi"].ToString(), strDescrpcion, TipoDestinoCuentaOperacion.Propia);
                }

                double dblItf = 0.0;
                double dblComisiones = 0.0;
                double dblTotalDebitar = 0.0;

                dblItf = (double)dsOut.Tables["OData"].Rows[0]["ODItf"];
                dblComisiones = (double)dsOut.Tables["OData"].Rows[0]["ODComi"];
                dblTotalDebitar = (double)dsOut.Tables["OData"].Rows[0]["ODttoor"];

                fechaOperacion = DateTime.Now;
                codOperacionGenerado = (string)dsHeader.Tables["OHead"].Rows[0]["OHlogtx"].ToString();

                //base.RefillData();

                //InsertaLogMonitoreo(Session["strCodigoClienteIBS"].ToString(), Session["strPad"].ToString().PadRight(19, ' '), TipoTransaccion.Transferencias(), TipoOperacionesMonitoreo.TransferenciasCuentasPropias(), System.Web.HttpContext.Current.Session.SessionID.ToString(), 2, _strError, string.Empty);
            }
            else
            {
                dsOut = null;
                dsHeader = null;
                tx = null;

                await DialogService.DisplayAlertAsync("Validación", "Código de error IBS: " + _strError, "OK");

                return;
                //InsertaLogMonitoreo(Session["strCodigoClienteIBS"].ToString(), Session["strPad"].ToString().PadRight(19, ' '), TipoTransaccion.Transferencias(), TipoOperacionesMonitoreo.TransferenciasCuentasPropias(), System.Web.HttpContext.Current.Session.SessionID.ToString(), 2, _strError, string.Empty);
            }
            dsOut = null;
            dsHeader = null;
            tx = null;


            //

            //acciones mock
            string rptaTrx = CuentaService.efectuarMovimiento(ctaOrigen, montoDec, moneda.Codigo, false);

            if (rptaTrx != "")
            {
                await DialogService.DisplayAlertAsync(Constantes.MSJ_INFO, rptaTrx, Constantes.MSJ_BOTON_OK);
            }
            else
            {
                string rptaTrx2 = CuentaService.efectuarMovimiento(ctaDestino, montoDec, moneda.Codigo, true);

                if (rptaTrx != "")
                {
                    await DialogService.DisplayAlertAsync(Constantes.MSJ_INFO, rptaTrx, Constantes.MSJ_BOTON_OK);
                }
                else
                {
                    RegistrarOperacionFrecuente(fechaOperacion);

                    parametros.Add("CodOperacionGenerado", codOperacionGenerado);
                    parametros.Add("FechaOperacion", fechaOperacion);

                    //await DialogService.DisplayAlertAsync(Constantes.MSJ_INFO, Constantes.MSJ_EXITO, Constantes.MSJ_BOTON_OK);
                    NavigationService.
                    await NavigationService.NavigateAsync("app:///NavBar//NavigationPage//Operaciones//TransfResumen", parametros); //de esta forma cuando vaya hacia atras volvera al home
                }

            }
        }

        public async Task AccionCtaTercero()
        {
            NavigationParameters parametros = GetNavigationParameters();
            parametros.Add(Constantes.pageOrigen, Constantes.pageTransfConfirmacion);

            Operacion operacion = parametros["Operacion"] as Operacion;
            SubOperacion suboperacion = parametros["SubOperacion"] as SubOperacion;

            string monto = parametros["Monto"] as string;
            Catalogo moneda = parametros["Moneda"] as Catalogo;
            Cuenta ctaOrigen = parametros["CtaCargo"] as Cuenta;
            Cuenta ctaDestino = parametros["CtaDestino"] as Cuenta;

            decimal montoDec = decimal.Parse(monto);
            string codOperacionGenerado = "";
            DateTime fechaOperacion;

            //seccion IBS
            string _strError = "";
            double dblMontoReal = System.Math.Round(double.Parse(monto), 2);
            string strMontoReal = Convert.ToString(System.Math.Round(dblMontoReal, 2) * 100).PadLeft(14, '0');

            string tlog = DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0');

            string strMensaje = '%' + ctaOrigen.NombreCta.PadLeft(12, '0') + '%' + tlog + '%' + monto.PadLeft(14, '0') + '%' + ctaDestino.NombreCta.PadLeft(12, '0') + '%' + moneda.Codigo + "".PadRight(30, ' ') + '0' + "".PadLeft(48, ' ') + strMontoReal;

            Transacciones tx = new Transacciones();
            DataSet dsOut = tx.EjecutarTransaccion(ListaTransacciones.TrasferenciaEjecuta, 250, strMensaje, ListaTransacciones.NombreMensajeOut(), ListaTransacciones.PosicionInicialCorte(), out _strError);
            DataSet dsHeader = tx.ObtenerCabecera(ListaTransacciones.TrasferenciaEjecuta, ListaTransacciones.NombreMensajeOut(), 0);
            if (_strError == "0000")
            {
                if (dsOut != null && dsHeader != null)
                {
                    //base.InsertaLogTransferencias(TipoTransaccion.Transferencias(), TipoOperacion.Transaccion(), DefaultValues.TrasferenciaEjecuta, strMensaje, dsOut, _strError, dsHeader.Tables["OHead"].Rows[0]["OHlogtx"].ToString(), ddlCuentaOrigen.SelectedValue.Substring(4, 12), ddlCuentaOrigen.SelectedItem.Text + '|' + dsOut.Tables["OData"].Rows[0]["ODclifr"].ToString(), dsOut.Tables["OData"].Rows[0]["ODmtoor"].ToString(), strMonedaOrigen, ddlCuentaDestino.SelectedValue.Substring(4, 12), " | " + ddlCuentaDestino.SelectedItem.Text + " | " + dsOut.Tables["OData"].Rows[0]["ODclito"].ToString(), dsOut.Tables["OData"].Rows[0]["ODmtocv"].ToString(), strMonedaDestino, string.Empty, string.Empty, dsOut.Tables["OData"].Rows[0]["ODticam"].ToString(), dsOut.Tables["OData"].Rows[0]["ODItf"].ToString(), dsOut.Tables["OData"].Rows[0]["ODComi"].ToString(), strDescrpcion, TipoDestinoCuentaOperacion.Propia);
                }

                double dblItf = 0.0;
                double dblComisiones = 0.0;
                double dblTotalDebitar = 0.0;

                dblItf = (double)dsOut.Tables["OData"].Rows[0]["ODItf"];
                dblComisiones = (double)dsOut.Tables["OData"].Rows[0]["ODComi"];
                dblTotalDebitar = (double)dsOut.Tables["OData"].Rows[0]["ODttoor"];

                fechaOperacion = DateTime.Now;
                codOperacionGenerado = (string)dsHeader.Tables["OHead"].Rows[0]["OHlogtx"].ToString();

                //base.RefillData();

                //InsertaLogMonitoreo(Session["strCodigoClienteIBS"].ToString(), Session["strPad"].ToString().PadRight(19, ' '), TipoTransaccion.Transferencias(), TipoOperacionesMonitoreo.TransferenciasCuentasPropias(), System.Web.HttpContext.Current.Session.SessionID.ToString(), 2, _strError, string.Empty);
            }
            else
            {
                dsOut = null;
                dsHeader = null;
                tx = null;

                await DialogService.DisplayAlertAsync("Validación", "Código de error IBS: " + _strError, "OK");

                return;
                //InsertaLogMonitoreo(Session["strCodigoClienteIBS"].ToString(), Session["strPad"].ToString().PadRight(19, ' '), TipoTransaccion.Transferencias(), TipoOperacionesMonitoreo.TransferenciasCuentasPropias(), System.Web.HttpContext.Current.Session.SessionID.ToString(), 2, _strError, string.Empty);
            }
            dsOut = null;
            dsHeader = null;
            tx = null;


            //

            //acciones mock
            string rptaTrx = CuentaService.efectuarMovimiento(ctaOrigen, montoDec, moneda.Codigo, false);

            if (rptaTrx != "")
            {
                await DialogService.DisplayAlertAsync(Constantes.MSJ_INFO, rptaTrx, Constantes.MSJ_BOTON_OK);
            }
            else
            {
                
                RegistrarOperacionFrecuente(fechaOperacion);

                parametros.Add("CodOperacionGenerado", codOperacionGenerado);
                parametros.Add("FechaOperacion", fechaOperacion);

                //await DialogService.DisplayAlertAsync(Constantes.MSJ_INFO, Constantes.MSJ_EXITO, Constantes.MSJ_BOTON_OK);
                await NavigationService.NavigateAsync("app:///NavBar//Operaciones//TransfResumen", parametros); //de esta forma cuando vaya hacia atras volvera al home
                
            }
        }

        public void RegistrarOperacionFrecuente(DateTime fechaOperacion)
        {
            if (IsOperacionFrecuente)
            {
                string pageOrigen = RefNavParameters[Constantes.pageOrigen] as string;
                Operacion operacion = RefNavParameters["Operacion"] as Operacion;
                SubOperacion suboperacion = RefNavParameters["SubOperacion"] as SubOperacion;

                string monto = RefNavParameters["Monto"] as string;
                Catalogo moneda = RefNavParameters["Moneda"] as Catalogo;
                Cuenta ctaOrigen = RefNavParameters["CtaCargo"] as Cuenta;
                Cuenta ctaDestino = RefNavParameters["CtaDestino"] as Cuenta;

                OperacionFrecuente opeFrec = new OperacionFrecuente
                {
                    FechaOperacion = fechaOperacion,
                    SubOperacion = suboperacion,
                    Operacion = operacion,
                    NombreFrecuente = NomOpeFrec
                };

                if (operacion.Id == "3" && suboperacion.Id == "2") //cta prpia
                {
                    opeFrec.CtaOrigen = ctaOrigen;
                    opeFrec.CtaDestino = ctaDestino;
                    opeFrec.Moneda = moneda;
                }
                else if (operacion.Id == "3" && suboperacion.Id == "0") //otra cta financiero
                {
                    opeFrec.CtaOrigen = ctaOrigen;
                    opeFrec.CtaDestino = ctaDestino;
                    opeFrec.Moneda = moneda;
                }

                OperacionService.AgregarOperacionFrecuente(opeFrec);
                EventAggregator.GetEvent<OpeFrecuenteAddedEvent>().Publish();
            }
        }
    }
}
