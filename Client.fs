namespace AgriConnect

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open System

[<JavaScript>]
module Client =

    // --- TEMPLATE BINDING ---
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    // --- TYPES ---
    type Language = 
        | English 
        | Lao

    type Page =
        | Home
        | Market
        | Dashboard
        | Payments

    type ProductCategory = Vegetables | Fruits | Grains | Livestock

    type Product = {
        Id: string
        Name: string
        Price: float
        Category: string
        FarmerName: string
        Description: string
    }

    type PaymentStatus = Pending | Paid

    type Payment = {
        Id: string
        BuyerName: string
        ProductName: string
        Amount: float
        DueDate: string
        Status: PaymentStatus
        FarmerId: string
    }

    // --- STATE & MOCK DATA ---
    let CurrentLang = Var.Create English
    let CurrentPage = Var.Create Home

    let initialProducts = [
        { Id = "p1"; Name = "Organic Tomatoes"; Price = 25000.0; Category = "Vegetables"; FarmerName = "Somboun"; Description = "Fresh organic tomatoes harvested today." }
        { Id = "p2"; Name = "Jasmine Rice (10kg)"; Price = 150000.0; Category = "Grains"; FarmerName = "Khamla"; Description = "Premium grade A jasmine rice from Vientiane province." }
        { Id = "p3"; Name = "Mangoes"; Price = 30000.0; Category = "Fruits"; FarmerName = "Bounmy"; Description = "Sweet yellow mangoes." }
        { Id = "p4"; Name = "Free-range Chicken"; Price = 45000.0; Category = "Livestock"; FarmerName = "Phouvieng"; Description = "Healthy and well-fed local chickens." }
    ]

    let initialPayments = [
        { Id = "pym1"; BuyerName = "Restaurant LanXang"; ProductName = "Jasmine Rice (10kg)"; Amount = 300000.0; DueDate = "2026-03-25"; Status = Pending; FarmerId = "f1" }
        { Id = "pym2"; BuyerName = "Market Vendor A"; ProductName = "Organic Tomatoes"; Amount = 125000.0; DueDate = "2026-03-15"; Status = Pending; FarmerId = "f1" }
        { Id = "pym3"; BuyerName = "Hotel Vientiane"; ProductName = "Mangoes"; Amount = 150000.0; DueDate = "2026-03-10"; Status = Paid; FarmerId = "f1" }
    ]

    // --- TRANSLATION DICTIONARY ---

    let dictEnLao en lo = fun (lang: Language) -> match lang with | English -> en | Lao -> lo
    
    // Reactive translation helper
    let tr en lo = CurrentLang.View.Map (dictEnLao en lo)

    // --- LOCAL STORAGE MOCK ---
    // In a real app we'd use JS.Window.LocalStorage, but we mock it via reactive variables for simplicity here
    // However, I will implement a rudimentary generic store wrapper.
    type Store<'T> = {
        Get: unit -> 'T list
        Set: 'T list -> unit
        Observe: unit -> View<'T list>
        Add: 'T -> unit
        Remove: ('T -> bool) -> unit
        Update: ('T -> bool) -> ('T -> 'T) -> unit
    }

    let createStore keySelector (initial: 'T list) =
        let model = ListModel.Create keySelector initial
        {
            Get = fun () -> model.Value |> List.ofSeq
            Set = fun items -> model.Set items
            Observe = fun () -> model.View |> View.Map List.ofSeq
            Add = fun item -> model.Add item
            Remove = fun predicate -> model.RemoveBy predicate
            Update = fun predicate updater ->
                let itemOpt = model.Value |> Seq.tryFind predicate
                match itemOpt with
                | Some item -> 
                    model.RemoveBy predicate
                    model.Add (updater item)
                | None -> ()
        }

    let ProductStore = createStore (fun (p: Product) -> p.Id) initialProducts
    let PaymentStore = createStore (fun (p: Payment) -> p.Id) initialPayments

    // --- STATISTICS & COMPUTED STATE ---
    let TotalSalesView =
        PaymentStore.Observe()
        |> View.Map (fun payments ->
            payments 
            |> List.filter (fun p -> p.Status = Paid)
            |> List.sumBy (fun p -> p.Amount)
        )

    let PendingSalesView =
        PaymentStore.Observe()
        |> View.Map (fun payments ->
            payments 
            |> List.filter (fun p -> p.Status = Pending)
            |> List.sumBy (fun p -> p.Amount)
        )

    let TotalProductsView =
        ProductStore.Observe()
        |> View.Map (fun products -> products.Length)

    // --- UI HELPERS ---
    
    let formatMoney (amount: float) =
        amount.ToString("N0") + " LAK"

    // --- VIEWS ---

    let EmptyState (msg: string) =
        IndexTemplate.EmptyStateView()
            .Message(msg)
            .Doc()

    let HomeView () =
        IndexTemplate.HomeView()
            .HeroTitle(tr "Welcome to AgriConnect" "ຍິນດີຕ້ອນຮັບສູ່ AgriConnect")
            .HeroSubtitle(tr "Directly connecting local farmers with buyers. Fresher produce, fairer prices." "ເຊື່ອມຕໍ່ຊາວກະສິກອນທ້ອງຖິ່ນກັບຜູ້ຊື້ໂດຍກົງ. ຜະລິດຕະພັນສົດໃໝ່, ລາຄາຍຸດຕິທໍາ.")
            .HeroCTA(tr "Explore Marketplace" "ສຳຫຼວດຕະຫຼາດ")
            .GoMarket(fun _ -> CurrentPage.Value <- Market)
            .Feature1Title(tr "For Farmers" "ສຳລັບຊາວກະສິກອນ")
            .Feature1Desc(tr "List your produce easily and reach more buyers locally and nationally." "ລົງລາຍການຜະລິດຕະພັນຂອງທ່ານໄດ້ຢ່າງງ່າຍດາຍ ແລະ ເຂົ້າເຖິງຜູ້ຊື້ຫຼາຍຂຶ້ນ.")
            .Feature2Title(tr "For Buyers" "ສຳລັບຜູ້ຊື້")
            .Feature2Desc(tr "Source fresh ingredients directly from the source without middlemen." "ຊອກຫາສ່ວນປະກອບສົດໃໝ່ໂດຍກົງຈາກແຫຼ່ງໂດຍບໍ່ມີຄົນກາງ.")
            .Feature3Title(tr "Secure Payments" "ການຈ່າຍເງິນທີ່ປອດໄພ")
            .Feature3Desc(tr "Track pending invoices and send convenient WhatsApp reminders." "ຕິດຕາມໃບເກັບເງິນທີ່ຍັງຄ້າງ ແລະ ສົ່ງແຈ້ງເຕືອນຜ່ານ WhatsApp.")
            .Doc()

    let MarketView () =
        let searchQuery = Var.Create ""
        let selectedCategory = Var.Create "All"
        
        // Advanced filtering logic
        let filteredProducts =
            View.Map3 (fun query category products ->
                products
                |> List.filter (fun p ->
                    let matchesCategory = 
                        if category = "All" then true
                        else p.Category = category
                    let matchesSearch =
                        if String.IsNullOrWhiteSpace(query) then true
                        else p.Name.ToLower().Contains(query.ToLower()) || p.FarmerName.ToLower().Contains(query.ToLower())
                    matchesCategory && matchesSearch
                )
            ) searchQuery.View selectedCategory.View (ProductStore.Observe())

        IndexTemplate.MarketView()
            .MarketTitle(tr "Marketplace" "ຕະຫຼາດ")
            .MarketSubtitle(tr "Discover fresh produce available today." "ຄົ້ນພົບຜະລິດຕະພັນສົດໆທີ່ມີໃນມື້ນີ້.")
            .SearchQuery(searchQuery)
            .SearchPlaceholder(tr "Search products or farmers..." "ຊອກຫາຜະລິດຕະພັນ ຫຼື ຊາວກະສິກອນ...")
            .SelectedCategory(selectedCategory)
            .ProductCards(
                filteredProducts
                |> Doc.BindSeqCached (fun p ->
                    IndexTemplate.ProductCard()
                        .ProductName(p.Name)
                        .Category(p.Category)
                        .Price(formatMoney p.Price)
                        .FarmerName(p.FarmerName)
                        .Description(p.Description)
                        .BuyLabel(tr "Contact Seller" "ຕິດຕໍ່ຜູ້ຂາຍ")
                        .BtnClass("btn-primary")
                        .BuyProduct(fun _ -> JS.Alert ("Contacting " + p.FarmerName + " for " + p.Name))
                        .Doc()
                )
            )
            .EmptyState(
                filteredProducts |> View.Map (fun lst -> 
                    if List.isEmpty lst then EmptyState "No products match your search." else Doc.Empty
                )
                |> Doc.EmbedView
            )
            .Doc()

    let DashboardView () =
        let newName = Var.Create ""
        let newPrice = Var.Create ""
        let newCat = Var.Create "Vegetables"
        
        IndexTemplate.DashboardView()
            .DashboardTitle(tr "Farmer Dashboard" "ໜ້າປັດຊາວກະສິກອນ")
            .AddProductTitle(tr "Add New Product" "ເພີ່ມຜະລິດຕະພັນໃໝ່")
            .LblName(tr "Product Name" "ຊື່ຜະລິດຕະພັນ")
            .PhName(tr "e.g. Fresh Mangoes" "ຕົວຢ່າງ: ໝາກມ່ວງສົດ")
            .LblPrice(tr "Price (LAK)" "ລາຄາ (ກີບ)")
            .PhPrice(tr "e.g. 15000" "ຕົວຢ່າງ: 15000")
            .LblCategory(tr "Category" "ປະເພດ")
            .BtnAdd(tr "List Product" "ລົງລາຍການຜະລິດຕະພັນ")
            .MyProductsTitle(tr "My Active Listings" "ລາຍການຜະລິດຕະພັນຂອງຂ້ອຍ")
            .TotalSalesLabel(tr "Total Earned:" "ຍອດຂາຍລວມ:")
            .TotalSalesVal(TotalSalesView |> View.Map formatMoney)
            .TotalPendingLabel(tr "Pending:" "ຍັງຄ້າງຈ່າຍ:")
            .TotalPendingVal(PendingSalesView |> View.Map formatMoney)
            .TotalProductsLabel(tr "Products:" "ຜະລິດຕະພັນ:")
            .TotalProductsVal(TotalProductsView |> View.Map string)
            .InputName(newName)
            .InputPrice(newPrice)
            .InputCategory(newCat)
            .AddProduct(fun _ ->
                if newName.Value <> "" && newPrice.Value <> "" then
                    let prf = float newPrice.Value
                    let prod : Product = {
                        Id = Guid.NewGuid().ToString()
                        Name = newName.Value
                        Price = prf
                        Category = newCat.Value
                        FarmerName = "You"
                        Description = "Newly added product."
                    }
                    ProductStore.Add(prod)
                    newName.Value <- ""
                    newPrice.Value <- ""
                else
                    JS.Alert("Please fill in all fields.")
            )
            .MyProductsList(
                ProductStore.Observe()
                |> Doc.BindSeqCached (fun p ->
                    IndexTemplate.DashboardListItem()
                        .ProductName(p.Name)
                        .Price(formatMoney p.Price)
                        .Status(tr "Active" "ເປີດໃຊ້ຢູ່")
                        .LblDelete(tr "Remove" "ລຶບ")
                        .RemoveProduct(fun _ -> ProductStore.Remove(fun x -> x.Id = p.Id))
                        .Doc()
                )
            )
            .EmptyState(
                ProductStore.Observe() |> View.Map (fun lst -> 
                    if List.isEmpty lst then EmptyState "You do not have any active listings." else Doc.Empty
                )
                |> Doc.EmbedView
            )
            .Doc()

    let PaymentsView () =
        let newBuyer = Var.Create ""
        let newProdName = Var.Create ""
        let newAmount = Var.Create ""
        let newDueDate = Var.Create (DateTime.Now.AddDays(7.0).ToString("yyyy-MM-dd"))
        
        IndexTemplate.PaymentsView()
            .PaymentsTitle(tr "Payments & Reminders" "ການຈ່າຍເງິນ ແລະ ແຈ້ງເຕືອນ")
            .PaymentsSubtitle(tr "Track your pending invoices and send WhatsApp reminders to buyers." "ຕິດຕາມໃບເກັບເງິນທີ່ຍັງຄ້າງ ແລະ ສົ່ງແຈ້ງເຕືອນຜ່ານ WhatsApp ໃຫ້ຜູ້ຊື້.")
            
            // New Payment Form
            .AddPaymentTitle(tr "Create New Invoice" "ສ້າງໃບເກັບເງິນໃໝ່")
            .LblBuyer(tr "Buyer Name" "ຊື່ຜູ້ຊື້")
            .LblProductName(tr "Product" "ຜະລິດຕະພັນ")
            .LblAmount(tr "Amount (LAK)" "ຈຳນວນເງິນ (ກີບ)")
            .LblDueDate(tr "Due Date" "ວັນກຳນົດຈ່າຍ")
            .BtnAddPayment(tr "Create Invoice" "ສ້າງໃບເກັບເງິນ")
            .InputBuyer(newBuyer)
            .InputProdName(newProdName)
            .InputAmount(newAmount)
            .InputDueDate(newDueDate)
            // Fix: Replaced .AddPayment reference with .CreatePayment logic to match index.html bindings
            .CreatePayment(fun _ ->
                if newBuyer.Value <> "" && newAmount.Value <> "" then
                    let amt = float newAmount.Value
                    let payment : Payment = {
                        Id = Guid.NewGuid().ToString()
                        BuyerName = newBuyer.Value
                        ProductName = newProdName.Value
                        Amount = amt
                        DueDate = newDueDate.Value
                        Status = Pending
                        FarmerId = "f1"
                    }
                    PaymentStore.Add(payment)
                    newBuyer.Value <- ""
                    newAmount.Value <- ""
                else
                    JS.Alert("Please fill in Buyer and Amount.")
            )
            
            .PaymentsList(
                PaymentStore.Observe()
                |> Doc.BindSeqCached (fun pym ->
                    let isOverdue = 
                        pym.Status = Pending && (DateTime.Parse(pym.DueDate) < DateTime.Now)
                    let dangerClass = if isOverdue then "danger" else ""
                    
                    IndexTemplate.PaymentItem()
                        .BuyerName(pym.BuyerName)
                        .ProductName(pym.ProductName)
                        .Amount(formatMoney pym.Amount)
                        .DueDate(pym.DueDate)
                        .DangerClass(dangerClass)
                        .LblRemind(tr "WhatsApp" "WhatsApp")
                        .LblMarkPaid(
                            tr 
                                (if pym.Status = Paid then "Paid" else "Mark Paid") 
                                (if pym.Status = Paid then "ຈ່າຍແລ້ວ" else "ໝາຍວ່າຈ່າຍແລ້ວ")
                        )
                        .RemindDisabled(if pym.Status = Paid then "disabled" else "")
                        .PaidDisabled(if pym.Status = Paid then "disabled" else "")
                        .MarkPaid(fun _ ->
                            if pym.Status = Pending then
                                let updated = { pym with Status = Paid }
                                PaymentStore.Remove(fun x -> x.Id = pym.Id)
                                PaymentStore.Add(updated)
                        )
                        .SendReminder(fun _ ->
                            if pym.Status = Pending then
                                let msg = sprintf "Hello %s, this is a friendly reminder for the payment of %s for %s due on %s. Please make the payment soon. Thanks!" pym.BuyerName (formatMoney pym.Amount) pym.ProductName pym.DueDate
                                let url = "https://wa.me/?text=" + JS.EncodeURIComponent(msg)
                                JS.Window.Open(url, "_blank") |> ignore
                        )
                        .Doc()
                )
            )
            .EmptyState(
                PaymentStore.Observe() |> View.Map (fun lst -> 
                    if List.isEmpty lst then EmptyState "No payment records." else Doc.Empty
                )
                |> Doc.EmbedView
            )
            .Doc()


    // --- MAIN SPA ENTRY POINT ---
    [<SPAEntryPoint>]
    let Main () =
        
        let viewMap =
            CurrentPage.View.Map (fun page ->
                match page with
                | Home -> HomeView ()
                | Market -> MarketView ()
                | Dashboard -> DashboardView ()
                | Payments -> PaymentsView ()
            )

        let isActive p1 p2 =
            if p1 = p2 then "active" else ""
            
        IndexTemplate.Main()
            .CurrentLangIcon(CurrentLang.View.Map (fun l -> match l with | English -> "🇱🇦 LAO" | Lao -> "🇬🇧 ENG"))
            .ToggleLang(fun _ -> CurrentLang.Value <- match CurrentLang.Value with | English -> Lao | Lao -> English)
            
            // Navigation translations
            .NavHome(tr "Home" "ໜ້າຫຼັກ")
            .NavMarket(tr "Marketplace" "ຕະຫຼາດ")
            .NavDashboard(tr "Dashboard" "ໜ້າປັດ")
            .NavPayments(tr "Payments" "ການຈ່າຍເງິນ")
            
            // Active Styles
            .HomeActive(CurrentPage.View.Map(fun p -> isActive p Home))
            .MarketActive(CurrentPage.View.Map(fun p -> isActive p Market))
            .DashboardActive(CurrentPage.View.Map(fun p -> isActive p Dashboard))
            .PaymentsActive(CurrentPage.View.Map(fun p -> isActive p Payments))
            
            // Click Handlers
            .GoHome(fun _ -> CurrentPage.Value <- Home)
            .GoMarket(fun _ -> CurrentPage.Value <- Market)
            .GoDashboard(fun _ -> CurrentPage.Value <- Dashboard)
            .GoPayments(fun _ -> CurrentPage.Value <- Payments)
            
            // Body Rendering
            .CurrentView(viewMap |> Doc.EmbedView)
            
            .Doc()
        |> Doc.RunById "main"
