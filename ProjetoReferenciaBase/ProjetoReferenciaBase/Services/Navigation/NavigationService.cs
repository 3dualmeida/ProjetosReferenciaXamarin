﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProjetoReferenciaBase.Controls;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;
using ProjetoReferenciaBase.ViewModels;

namespace ProjetoReferenciaBase.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        protected Application CurrentApplication
        {
            get { return Application.Current; }
        }

        public async Task InitializeAsync<TViewModel>(NavigationParameters parameters = null, bool navigationPage = false) where TViewModel : ViewModelBase
        => await InternalInitAsync(typeof(TViewModel), parameters, navigationPage);


        public Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase
            => InternalNavigateToAsync(typeof(TViewModel), null);

        public Task NavigateToAsync<TViewModel>(NavigationParameters parameters) where TViewModel : ViewModelBase
            => InternalNavigateToAsync(typeof(TViewModel), parameters);

        public Task NavigateToAsync(Type viewModelType)
            => InternalNavigateToAsync(viewModelType, null);

        public Task NavigateToAsync(Type viewModelType, NavigationParameters parameters)
            => InternalNavigateToAsync(viewModelType, parameters);


        public Task PushModalToAsync<TViewModel>() where TViewModel : ViewModelBase
              => InternalNavigateToAsync(typeof(TViewModel), null, true);

        public Task PushModalToAsync<TViewModel>(NavigationParameters parameters) where TViewModel : ViewModelBase
          => InternalNavigateToAsync(typeof(TViewModel), parameters, true);



        public async Task NavigateBackAsync(NavigationParameters parameters = null)
        {
            if (CurrentApplication.MainPage != null)
            {

                Xamarin.Forms.Page page;
                if (CurrentApplication.MainPage.Navigation.ModalStack.Count > 0)
                    page = await CurrentApplication.MainPage.Navigation.PopModalAsync();
                else
                    page = await CurrentApplication.MainPage.Navigation.PopAsync();

                if (parameters == null)
                {
                    parameters = new NavigationParameters();
                }

                parameters.NavigationState = NavigationState.Backward;

                if (page.NavigationArgs() != null && page.NavigationArgs().Count > 0)
                {
                    page.RemoveNavigationArgs();
                }

                page.AddNavigationArgs(parameters);


                var viewmodel = page.BindingContext as ViewModelBase;
                if (viewmodel != null)
                    await viewmodel.OnNavigate(parameters);

            }
        }

        public async Task NavigateParentBackAsync(NavigationParameters parameters = null)
        {
            if (CurrentApplication.MainPage != null)
            {

                Xamarin.Forms.Page page;
                if (CurrentApplication.MainPage.Navigation.ModalStack.Count > 0)
                    page = await CurrentApplication.MainPage.Navigation.PopModalAsync();
                else
                    page = await CurrentApplication.MainPage.Navigation.PopAsync();

                var parentpage = GetPreviousPage(page, CurrentApplication.MainPage.Navigation.NavigationStack);

                if (parameters == null)
                {
                    parameters = new NavigationParameters();
                }

                parameters.NavigationState = NavigationState.Backward;

                var viewmodel = parentpage.BindingContext as ViewModelBase;
                if (viewmodel != null)
                    await viewmodel.OnNavigate(parameters);

            }
        }

        public static Page GetPreviousPage(Page currentPage, System.Collections.Generic.IReadOnlyList<Page> navStack)
        {
            Page previousPage = null;

            int previousPageIndex = GetPreviusPageIndex(currentPage, navStack);
            if (navStack.Count >= 0 && previousPageIndex >= 0)
                previousPage = navStack[previousPageIndex];

            return previousPage;
        }

        public static int GetPreviusPageIndex(Page currentPage, System.Collections.Generic.IReadOnlyList<Page> navStack)
        {
            int stackCount = navStack.Count;
            for (int x = 0; x < stackCount; x++)
            {
                var view = navStack[x];
                if (view == currentPage)
                    return x;
            }

            return stackCount - 1;
        }



        public async Task NavigateUriAsync(Uri uri, bool clearBackStack, NavigationParameters parameters = null)
        {
            try
            {

                var segments = GetUriSegments(uri);
                var nextSegment = segments.Dequeue();

                Page page = await InternalNavigateUriAsync(nextSegment, segments, true, parameters);


                if (parameters == null)
                {
                    parameters = new NavigationParameters();
                }

                parameters.NavigationState = NavigationState.Init;

                var viewmodel = page.BindingContext as ViewModelBase;
                if (viewmodel != null)
                    await viewmodel.OnNavigate(parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"NavigateAndClearBackStackAsync: {ex.Message}");
            }
        }

        private async Task<Page> InternalNavigateUriAsync(string nextSegment, Queue<string> segments, bool firstPage,
            NavigationParameters parameters = null)
        {

            Type viewmodel = ViewModelLocator.Current.Mappings.Where(x =>
             x.Value.Name == nextSegment).FirstOrDefault().Key;

            Xamarin.Forms.Page page = CreateAndBindPage(viewmodel, parameters);
            var navigationPage = CurrentApplication.MainPage as SampleNavigationPage;

            var currentNavigationPage = CurrentApplication.MainPage as SampleNavigationPage;

            if (currentNavigationPage != null && !firstPage)
            {
                await navigationPage.PushAsync(page, false);
            }
            else
            {
                CurrentApplication.MainPage = new SampleNavigationPage(page);
            }

            if (segments.Count != 0)
            {
                var next = segments.Dequeue();
                return await InternalNavigateUriAsync(next, segments, false, parameters);
            }
            else
                return page;

        }

        public async Task NavigateAndClearBackStackAsync<TViewModel>(NavigationParameters parameters = null, bool animated = false) where TViewModel : ViewModelBase
        {
            try
            {
                Xamarin.Forms.Page page = CreateAndBindPage(typeof(TViewModel), parameters);
                var navigationPage = CurrentApplication.MainPage as SampleNavigationPage;

                await navigationPage.PushAsync(page, animated);

                await (page.BindingContext as ViewModelBase).LoadAsync(parameters);

                if (navigationPage != null && navigationPage.Navigation.NavigationStack.Count > 0)
                {
                    var existingPages = navigationPage.Navigation.NavigationStack.ToList();

                    foreach (var existingPage in existingPages)
                    {
                        if (existingPage != page)
                            navigationPage.Navigation.RemovePage(existingPage);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"NavigateAndClearBackStackAsync: {ex.Message}");
            }
        }

        async Task InternalNavigateToAsync(Type viewModelType, NavigationParameters parameters, bool modal = false)
        {
            Xamarin.Forms.Page page = CreateAndBindPage(viewModelType, parameters);

            var currentNavigationPage = CurrentApplication.MainPage as SampleNavigationPage;

            if (currentNavigationPage != null)
            {

                if (modal)
                    await CurrentApplication.MainPage.Navigation.PushModalAsync(page);
                else
                    await currentNavigationPage.PushAsync(page);
            }
            else
            {
                CurrentApplication.MainPage = new SampleNavigationPage(page);
            }

            await ParameterNavigation(page, parameters, NavigationState.Forward);
        }

        async Task InternalInitAsync(Type viewModelType, NavigationParameters parameters, bool navigationPage = false)
        {
            Xamarin.Forms.Page page = CreateAndBindPage(viewModelType, parameters);

            if (CurrentApplication.MainPage is SampleNavigationPage currentNavigationPage)
            {
                await currentNavigationPage.PushAsync(page);
            }
            else
            {
                if (navigationPage)
                {
                    CurrentApplication.MainPage = new SampleNavigationPage(page);
                }
                else
                    CurrentApplication.MainPage = page;

            }

            await ParameterNavigation(page, parameters, NavigationState.Init);

        }

        Type GetPageTypeForViewModel(Type viewModelType)
        {
            if (!ViewModelLocator.Current.Mappings.ContainsKey(viewModelType))
            {
                throw new KeyNotFoundException($"No map for ${viewModelType} was found on navigation mappings");
            }

            return ViewModelLocator.Current.Mappings[viewModelType];
        }

        Xamarin.Forms.Page CreateAndBindPage(Type viewModelType, NavigationParameters parameters)
        {
            Type pageType = GetPageTypeForViewModel(viewModelType);

            if (pageType == null)
            {
                throw new Exception($"Mapping type for {viewModelType} is not a page");
            }

            Xamarin.Forms.Page page = Activator.CreateInstance(pageType) as Xamarin.Forms.Page;
            try
            {
                ViewModelBase viewModel = App.ServiceProvider.GetService(viewModelType) as ViewModelBase;
                page.BindingContext = viewModel;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateAndBindPage error: {ex.Message}");
            }

            return page;
        }

        async Task ParameterNavigation(Xamarin.Forms.Page page, NavigationParameters parameters, NavigationState state)
        {
            if (parameters == null)
            {
                parameters = new NavigationParameters();
            }

            parameters.NavigationState = NavigationState.Init;

            page.AddNavigationArgs(parameters);

            await (page.BindingContext as ViewModelBase).LoadAsync(parameters);

            await (page.BindingContext as ViewModelBase).OnNavigate(parameters);
        }

        //URI Utils
        private static readonly char[] _pathDelimiter = { '/' };
        private static Queue<string> GetUriSegments(Uri uri)
        {
            var segmentStack = new Queue<string>();

            if (!uri.IsAbsoluteUri)
            {
                uri = EnsureAbsolute(uri);
            }

            string[] segments = uri.PathAndQuery.Split(_pathDelimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                segmentStack.Enqueue(Uri.UnescapeDataString(segment));
            }

            return segmentStack;
        }

        private static Uri EnsureAbsolute(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }

            if (!uri.OriginalString.StartsWith("/", StringComparison.Ordinal))
            {
                return new Uri("http://localhost/" + uri, UriKind.Absolute);
            }
            return new Uri("http://localhost" + uri, UriKind.Absolute);
        }
    }
}
