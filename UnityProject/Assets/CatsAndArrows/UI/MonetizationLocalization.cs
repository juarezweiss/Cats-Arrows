using System.Collections.Generic;

namespace CatsAndArrows.UI
{
    public static class MonetizationLocalization
    {
        public static void AddMessages(Dictionary<string, string> messages, int localeIndex)
        {
            var entries = new Dictionary<string, string[]>
            {
                { "privacy.options", new[] { "Privacy Options", "Opções de privacidade", "Opciones de privacidad" } },
                { "privacy.unavailable", new[] { "Privacy options are unavailable. Try again later. You can keep playing.", "As opções de privacidade estão indisponíveis. Tente mais tarde. Você pode continuar jogando.", "Las opciones de privacidad no están disponibles. Inténtalo más tarde. Puedes seguir jugando." } },
                { "notification.ok", new[] { "OK", "OK", "OK" } },
                { "monetization.hint-ads", new[] { "Hint (ads)", "Dica (anúncios)", "Pista (anuncios)" } },
                { "monetization.remove", new[] { "Remove Ads", "Remover anúncios", "Eliminar anuncios" } },
                { "monetization.restore", new[] { "Restore Purchases", "Restaurar compras", "Restaurar compras" } },
                { "monetization.redeem", new[] { "Redeem Offer Code", "Resgatar código de oferta", "Canjear código de oferta" } },
                { "monetization.status.OfferCodeUnsupported", new[] { "In-app offer codes require iOS 16.3 or later. Redeem in the App Store or use the redemption link provided with your code, then return to Paws.", "Códigos de oferta no app exigem iOS 16.3 ou posterior. Resgate na App Store ou use o link de resgate recebido com o código e volte ao Paws.", "Los códigos de oferta en la app requieren iOS 16.3 o posterior. Canjea en App Store o usa el enlace recibido con el código y vuelve a Paws." } },
                { "monetization.status.OfferCodeUnavailable", new[] { "Could not open offer code redemption. Try again later. You can keep playing.", "Não foi possível abrir o resgate de código. Tente mais tarde. Você pode continuar jogando.", "No se pudo abrir el canje de código. Inténtalo más tarde. Puedes seguir jugando." } },
                { "monetization.explanation", new[] { "Removes ads between puzzles and makes hints available without ads.", "Remove anúncios entre tabuleiros e libera dicas sem anúncios.", "Elimina anuncios entre tableros y permite pistas sin anuncios." } },
                { "monetization.review", new[] { "Mock purchases only. No store or charges.", "Compras apenas simuladas. Sem loja ou cobranças.", "Solo compras simuladas. Sin tienda ni cargos." } },
                { "monetization.status.None", new[] { "", "", "" } },
                { "monetization.status.AdUnavailable", new[] { "Ad unavailable. You can keep playing.", "Anúncio indisponível. Você pode continuar jogando.", "Anuncio no disponible. Puedes seguir jugando." } },
                { "monetization.status.RewardNotEarned", new[] { "No reward earned. You can try again.", "Recompensa não obtida. Você pode tentar de novo.", "No se obtuvo recompensa. Puedes intentarlo de nuevo." } },
                { "monetization.status.EarnedHint", new[] { "Your next hint is already earned.", "Sua próxima dica já foi conquistada.", "Tu próxima pista ya está ganada." } },
                { "monetization.status.RestoreRequired", new[] { "Check ownership with Restore Purchases.", "Verifique a compra em Restaurar compras.", "Verifica la compra con Restaurar compras." } },
                { "monetization.status.PurchasePending", new[] { "Purchase pending approval.", "Compra aguardando aprovação.", "Compra pendiente de aprobación." } },
                { "monetization.status.PurchaseUnavailable", new[] { "Purchase unavailable. Try restoring later.", "Compra indisponível. Tente restaurar mais tarde.", "Compra no disponible. Prueba a restaurar más tarde." } },
                { "monetization.status.Owned", new[] { "All ads removed", "Todos os anúncios removidos", "Todos los anuncios eliminados" } },
                { "monetization.status.StorageRecovery", new[] { "Could not save. Your game can continue.", "Não foi possível salvar. Você pode continuar jogando.", "No se pudo guardar. Puedes seguir jugando." } },
                { "monetization.status.Busy", new[] { "Please wait…", "Aguarde…", "Espera…" } }
            };
            foreach (var entry in entries) messages.Add(entry.Key, entry.Value[localeIndex]);
        }
    }
}
