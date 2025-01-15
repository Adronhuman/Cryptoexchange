var chartId = "chartContainer";
var isChartInitialized = false;

window.drawChart = (bids, asks) => {
    if (isChartInitialized) {
        initializeChart();
    } else {
        updateChart(bids, asks);
    }
}

function updateChart(bids, asks) {
    function extract(orders) {
        var prices = orders.map(order => order.price);
        var amounts = orders.map(order => order.amount);
        return { prices, amounts };
    }

    // data update
    var bidData = extract(bids), askData = extract(asks);

    Plotly.update(chartId, {
        x: [bidData.prices, askData.prices],
        y: [bidData.amounts, askData.amounts]
    });

    // layout adjustments
    var maxAmount = Math.max(...bids.map(x => x.amount), ...asks.map(x => x.amount));
    var prices = bids.map(x => x.price).concat(asks.map(x => x.price));
    var minPrice = Math.min(prices), maxPrice = Math.max(prices);
    var priceRangeSize = maxPrice - minPrice;

        // Scaling by 1.5 to ensure the highest bar is slightly higher than the midpoint
    amountRange = [0, 1.5 * maxAmount];
        // Expand the price range by 20% on both the lower and upper bounds
    priceRange = [minPrice - 0.2 * priceRangeSize, maxPrice + 0.2 * priceRangeSize];

    var layout = {
        xaxis: {
            range: priceRange
        },
        yaxis: {
            range: amountRange
        },
    };
    Plotly.relayout(chartId, layout);
}

function initializeChart(bids, asks) {
    var bidSeries = prepareSeries(bids, "green", "Bids");
    var askSeries = prepareSeries(asks, "red", "Asks");
        
    var amountRange = [0, 0.1], priceRange = [0, 100];

    if (bids.length > 0 || asks.length > 0) {

        var maxAmount = Math.max(...bids.map(x => x.amount), ...asks.map(x => x.amount));


        var prices = bids.map(x => x.price).concat(asks.map(x => x.price));
        var minPrice = Math.min(prices), maxPrice = Math.max(prices);
        var priceRangeSize = maxPrice - minPrice;

        // Scaling by 1.5 to ensure the highest bar is slightly higher than the midpoint
        amountRange = [0, 1.5 * maxAmount];
        // Expand the price range by 20% on both the lower and upper bounds
        priceRange = [minPrice - 0.2 * priceRangeSize, maxPrice + 0.2 * priceRangeSize];
    }


    var layout = {
        title: "BTC/EUR",
        barmode: "group",
        xaxis: {
            title: "Price in EUR",
            tickangle: -90,
            range: priceRange
        },
        yaxis: {
            title: "BTC Quantity",
            range: amountRange,
        },
        bargap: 0,
        bargroupgap: 0,
    };

    Plotly.newPlot(chartId, [bidSeries, askSeries], layout);
    isChartInitialized = true;
}

function prepareSeries(orders, color, label) {
    const WIDTHS = new Array(orders.length).fill(1);
    var prices = orders.map(order => order.price);
    var amounts = orders.map(order => order.amount);
    var amountLabels = amounts.map(x => x.toString());

    var series = {
        x: prices,
        y: amounts,
        type: "bar",
        name: `${label}`,
        marker: { color }, 
        text: amountLabels, // Labels to display above bars
        textposition: "outside",
        width: WIDTHS,
    };

    return series;
}