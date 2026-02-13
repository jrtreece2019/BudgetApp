// File download helper for CSV export.
// Creates a temporary <a> element with a data URI and clicks it to
// trigger a browser download. This works in both Blazor WebView (MAUI)
// and Blazor Server (Web).
window.fileDownload = {
    downloadCsv: function (filename, csvContent) {
        var blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        var url = URL.createObjectURL(blob);

        var link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.display = 'none';

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        URL.revokeObjectURL(url);
    }
};
