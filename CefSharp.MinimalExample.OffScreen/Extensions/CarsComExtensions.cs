using CefSharp.MinimalExample.OffScreen.Models;
using CefSharp.OffScreen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.OffScreen.Extensions
{

    public static class CarsComExtensions
    {
        public static async Task CarsComCheckWhetherSignedInOrNotAsync(this ChromiumWebBrowser page)
        {
            if (page.Address.StartsWith("https://www.cars.com/signin"))
            {
                // If not sign in!
                await page.EvaluateScriptAsync("let emailElement = document.querySelector(\"[name='user[email]']\");emailElement.value = \"johngerson808@gmail.com\"");
                await page.EvaluateScriptAsync("let passwordElement = document.querySelector(\"[name='user[password]']\");passwordElement.value = \"test8008\"");
                await page.EvaluateScriptAsync("let signInBtn = document.querySelector(\"button[type='submit']\"); signInBtn.click()");
                await page.WaitForNavigationAsync();
            }
        }

        public static async Task CarsComSetSearchParametersAsync(this ChromiumWebBrowser page, SearchParametersModel model)
        {
            string script = $@"
            let carType = document.getElementById(""make-model-search-stocktype"")
            carType.value = '{model.Car}'
            let makeType = document.getElementById(""makes"")
            makeType.value = '{model.Make}'
            let modelType = document.getElementById(""models"")
            modelType.value = '{model.Model}'
            let maxPrice = document.getElementById(""make-model-max-price"")
            maxPrice.value = '{model.MaxPrice}'
            let distance = document.getElementById(""make-model-maximum-distance"")
            distance.value = '{model.Distance}'
            let zip = document.getElementById(""make-model-zip"")
            zip.value = '{model.Zip}'
            let searchBtn = document.querySelector(""button[data-searchtype='make']"")
            searchBtn.click()
        ";
            await page.EvaluateScriptAsync(script);
            await page.WaitForNavigationAsync(TimeSpan.FromSeconds(60));
        }

        public static async Task CarsComCreateCarArrayAsync(this ChromiumWebBrowser page)
        {
            var createCarArrayJs = "var cars = [] ";
            await page.EvaluateScriptAsync(createCarArrayJs);
        }

        public static async Task CarsComGetCarListDataAsync(this ChromiumWebBrowser page)
        {
            var getCarsJs = @"
        {
            const vehicleCards = Array.from(document.querySelectorAll('.vehicle-card'));

            vehicleCards.forEach((vehicleCard, i) => {
                const vehicleDetails = vehicleCard.querySelector('.vehicle-card-main .vehicle-details');
                const title = vehicleDetails.querySelector('.vehicle-card-link h2.title').innerHTML;
                const mileage = vehicleDetails.querySelector('.mileage').innerHTML;
                const price = vehicleDetails.querySelector('.price-section .primary-price').innerHTML;
                const dealerName = vehicleDetails.querySelector('.vehicle-dealer .dealer-name strong').innerHTML;
                cars.push({
                    title,
                    mileage,
                    price,
                    dealerName
                })
            });
        }
        ";
            await page.EvaluateScriptAsync(getCarsJs);
        }

        public static async Task<List<CarModelAtList>> CarsComGetCarsAsync(this ChromiumWebBrowser page)
        {
            var carsJson = await page.EvaluateScriptAsync("JSON.stringify(cars)");
            return JsonConvert.DeserializeObject<List<CarModelAtList>>(carsJson.Result.ToString());
        }

        public static async Task CarsComGoNextPageAsync(this ChromiumWebBrowser page)
        {
            await page.EvaluateScriptAsync("let nextPageBtn = document.getElementById(\"next_paginate\"); nextPageBtn.click()");
            await page.WaitForNavigationAsync(TimeSpan.FromSeconds(60));
        }

        public static async Task<CarModelSpesific> CarsComGetLastCarDataAsync(this ChromiumWebBrowser page)
        {
            await page.EvaluateScriptAsync("const vehicleCards = Array.from(document.querySelectorAll('.vehicle-card'));");
            await page.EvaluateScriptAsync("let spesificCarBtn = vehicleCards[vehicleCards.length-1].querySelector(\".vehicle-card-main .vehicle-details .vehicle-card-link\"); spesificCarBtn.click()");
            await page.WaitForNavigationAsync(TimeSpan.FromSeconds(60));

            var spesificParserJs = @"
            var images = []
            const imagesElements = Array.from(document.querySelector('.modal-slides-and-controls').querySelectorAll('.swipe-main-image'));
            imagesElements.forEach((image, i) => {
                images.push(image.getAttribute('src'))
            })
        ";
            await page.EvaluateScriptAsync(spesificParserJs);
            var spesificCarImagesJson = await page.EvaluateScriptAsync("JSON.stringify(images)");
            var spesificCarImages = JsonConvert.DeserializeObject<List<string>>(spesificCarImagesJson.Result.ToString());

            await page.EvaluateScriptAsync("document.querySelector(\"div[data-linkname='price-badge-good']\").click()");
            var spesificCarDescription = await page.EvaluateScriptAsync("document.getElementById(\"sds-modal\").innerText");

            return new CarModelSpesific
            {
                Description = spesificCarDescription.Result.ToString(),
                Photos = spesificCarImages
            };
        }

        public static async Task<ParentModel> CarsComGetParentAsync(this ChromiumWebBrowser page, SearchParametersModel model)
        {
            await page.LoadUrlAsync("https://www.cars.com");
            await page.CarsComSetSearchParametersAsync(model);

            await page.CarsComCreateCarArrayAsync();
            await page.CarsComGetCarListDataAsync();
            var cars = await page.CarsComGetCarsAsync();
            await page.CarsComGoNextPageAsync();

            await page.CarsComCreateCarArrayAsync();
            await page.CarsComGetCarListDataAsync();

            cars.AddRange(await page.CarsComGetCarsAsync());
            var spesificCar = await page.CarsComGetLastCarDataAsync();

            return new ParentModel
            {
                AllCars = cars,
                SpesificCar = spesificCar
            };
        }
    }
}