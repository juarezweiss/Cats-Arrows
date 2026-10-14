using System.Collections.Generic;

namespace CatsAndArrows.UI
{
    public static class MonetizationLocalization
    {
        public static void AddMessages(Dictionary<string, string> messages, int localeIndex)
        {
            var entries = new Dictionary<string, string[]>
            {
                { "monetization.hint-ads", new[] { "Hint (ads)", "Dica (anúncios)", "Pista (anuncios)" } },
                { "monetization.remove", new[] { "Remove All Ads", "Remover todos os anúncios", "Eliminar todos los anuncios" } },
                { "monetization.restore", new[] { "Restore Purchases", "Restaurar compras", "Restaurar compras" } },
                { "monetization.explanation", new[] { "Removes ads between puzzles and makes hints available without ads.", "Remove anúncios entre tabuleiros e libera dicas sem anúncios.", "Elimina anuncios entre tableros y permite pistas sin anuncios." } },
                { "monetization.review", new[] { "Mock review only. No real ads, store or charges.", "Apenas teste simulado. Sem anúncios reais, loja ou cobranças.", "Solo prueba simulada. Sin anuncios reales, tienda ni cargos." } },
                { "monetization.rules", new[] { "Hints place one correct Cat or X. Without Remove All Ads, each hint requires a rewarded ad. An ad may appear when starting another puzzle after every fourth completed board.", "Dicas colocam um Gato ou X correto. Sem Remover todos os anúncios, cada dica exige um anúncio recompensado. Um anúncio pode aparecer ao iniciar outro tabuleiro após cada quarto tabuleiro concluído.", "Las pistas colocan un Gato o X correcto. Sin Eliminar todos los anuncios, cada pista requiere un anuncio con recompensa. Puede aparecer un anuncio al comenzar otro tablero después de cada cuarto tablero completado." } },
                { "monetization.status.None", new[] { "", "", "" } },
                { "monetization.status.AdUnavailable", new[] { "Ad unavailable. Try Hint again later.", "Anúncio indisponível. Tente a dica mais tarde.", "Anuncio no disponible. Prueba la pista más tarde." } },
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
            var freeHintSentence = new[] { "Hints are unlimited and immediately place one correct Cat or X.",
                "As dicas são ilimitadas e colocam imediatamente um Gato ou X correto.",
                "Las pistas son ilimitadas y colocan inmediatamente un Gato o X correcto." };
            messages.Add("monetization.rules-body", messages["game.rules-body"].Replace(freeHintSentence[localeIndex], "") +
                "\n\n" + messages["monetization.rules"]);
        }
    }
}
