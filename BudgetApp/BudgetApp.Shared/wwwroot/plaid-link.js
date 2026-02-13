/**
 * Plaid Link JavaScript interop for Blazor.
 *
 * Plaid Link is a client-side JavaScript widget that handles the entire
 * bank login flow (choosing a bank, entering credentials, selecting accounts).
 * It runs inside an iframe for security — your app never sees the user's
 * bank password.
 *
 * Since Blazor can't run JavaScript directly, we expose a PlaidLink.open()
 * function that Blazor calls via IJSRuntime. When the user finishes (or exits),
 * we call back into Blazor using the DotNetObjectReference.
 *
 * SETUP: The Plaid Link SDK script must be loaded in index.html / App.razor:
 *   <script src="https://cdn.plaid.com/link/v2/stable/link-initialize.js"></script>
 *
 * USAGE FROM BLAZOR:
 *   await JS.InvokeVoidAsync("PlaidLink.open", linkToken, dotNetRef);
 */
window.PlaidLink = {
    /**
     * Opens the Plaid Link widget.
     * @param {string} linkToken - The link token from POST /api/plaid/link-token
     * @param {object} dotNetRef - DotNetObjectReference to the Blazor component
     */
    open: function (linkToken, dotNetRef) {
        // Check if the Plaid script is loaded.
        if (typeof Plaid === 'undefined') {
            console.error('Plaid Link SDK not loaded. Add the script tag to index.html.');
            dotNetRef.invokeMethodAsync('OnPlaidExit', 'Plaid Link SDK not loaded.');
            return;
        }

        // Create and open the Plaid Link handler.
        const handler = Plaid.create({
            token: linkToken,

            // Called when the user successfully links a bank.
            onSuccess: function (publicToken, metadata) {
                // publicToken is the temporary token we send to our API.
                dotNetRef.invokeMethodAsync('OnPlaidSuccess', publicToken);
            },

            // Called when the user exits (cancels or encounters an error).
            onExit: function (err, metadata) {
                if (err) {
                    console.error('Plaid Link error:', err);
                    dotNetRef.invokeMethodAsync('OnPlaidExit', err.display_message || err.error_message || 'An error occurred.');
                } else {
                    // User cancelled — not an error.
                    dotNetRef.invokeMethodAsync('OnPlaidExit', null);
                }
            },

            // Called when specific events happen inside Plaid Link
            // (e.g., user selected a bank, entered credentials, etc.).
            // Useful for analytics; not required for basic functionality.
            onEvent: function (eventName, metadata) {
                console.log('Plaid Link event:', eventName);
            }
        });

        handler.open();
    }
};
