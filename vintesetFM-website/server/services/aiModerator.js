import { GoogleGenAI } from '@google/genai';

let ai = null;

export async function judgeTopic(title, content) {
  try {
    const apiKey = process.env.GEMINI_API_KEY;
    
    // Se o Manager ainda não configurou a Key no .env do Coolify,
    // nós jogamos pra moderação manual com segurança.
    if (!apiKey) {
      console.warn('[AI_MODERATOR] Chave GEMINI_API_KEY ausente. Tópico enviado para a Fila Manual (PENDING).');
      return { status: 'PENDING', reason: 'Fila Manual (Auto-Moderador Desativado por falta de Chave API)' };
    }

    if (!ai) {
      ai = new GoogleGenAI({ apiKey });
    }

    const prompt = `
Você é o Moderador Automático do fórum de games VintesetFM.
Seu trabalho é analisar o título e conteúdo do post abaixo.
Responda APENAS com um objeto JSON estrito com 2 chaves:
- "status": String. Retorne "APPROVED" se o post for inofensivo, falar sobre games, modding, downloads, futebol, dúvidas corriqueiras. Retorne "REJECTED" se for material tóxico grave, ódio, racismo, pirataria óbvia (exigindo cracks), ou spam suspeito. Retorne "PENDING" se não tiver certeza absoluta.
- "reason": String descrevendo brevemente o motivo (ex: "Post dentro das diretrizes", ou "Spam detectado", ou "Dúvida - Triagem humana necessária").

Título: ${title}
Conteúdo: ${content}

Retorne Pura e Exclusivamente o JSON, sem tags markdown ou comentários adicionais.
`;

    const response = await ai.models.generateContent({
      model: 'gemini-2.5-flash',
      contents: prompt,
      config: {
        responseMimeType: 'application/json',
      }
    });

    const resultText = response.text;
    const parsed = JSON.parse(resultText);

    if (['APPROVED', 'PENDING', 'REJECTED'].includes(parsed.status)) {
       console.log(`[AI_MODERATOR] Julgamento concluído -> Status: ${parsed.status} | Razão: ${parsed.reason}`);
       return { status: parsed.status, reason: parsed.reason };
    }
    
    return { status: 'PENDING', reason: 'A IA retornou um formato inesperado.' };

  } catch (error) {
    console.error('[AI_MODERATOR] Erro estrutural ou de rede na requisição do Gemini:', error);
    return { status: 'PENDING', reason: 'Erro na conexão com Google AI Studio. Triagem Manual.' };
  }
}
