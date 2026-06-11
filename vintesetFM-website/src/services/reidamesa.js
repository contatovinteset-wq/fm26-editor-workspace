// Infra do Rei da Mesa multistream (Fase 3c).
// A slug do criador vai na URL (/reidamesa/c/:slug/...). Estes helpers
// derivam a slug e injetam o header X-Creator-Slug nas chamadas de API,
// para o backend resolver o creator certo (sem slug => vinteset/Creator #1).

import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';

// Deriva a slug do criador a partir do path atual. Funciona em qualquer
// contexto (inclusive overlay, fora do React Router).
export function getCreatorSlug() {
  const m = window.location.pathname.match(/^\/reidamesa\/c\/([^/]+)/);
  return m ? decodeURIComponent(m[1]).toLowerCase() : null;
}

// fetch que injeta automaticamente o X-Creator-Slug para /api/reidamesa/*.
export function rdmFetch(url, init = {}) {
  const slug = getCreatorSlug();
  const headers = new Headers(init.headers || {});
  if (slug) headers.set('X-Creator-Slug', slug);
  return fetch(url, { ...init, headers });
}

// Base de URL para links internos do Rei da Mesa, ciente do criador atual.
// No contexto de um criador => /reidamesa/c/:slug ; senão => /reidamesa.
export function useRdmBase() {
  const { slug } = useParams();
  return slug ? `/reidamesa/c/${slug}` : '/reidamesa';
}

// Carrega o branding/identidade do criador atual (pela slug da rota).
// Sem slug (rota bare = vinteset) retorna creator nulo.
export function useCreator() {
  const { slug } = useParams();
  const [creator, setCreator] = useState(null);
  const [isLoading, setIsLoading] = useState(!!slug);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!slug) {
      setCreator(null);
      setIsLoading(false);
      setNotFound(false);
      return;
    }
    let alive = true;
    setIsLoading(true);
    fetch(`/api/reidamesa/creator/${encodeURIComponent(slug)}`)
      .then((r) => (r.ok ? r.json() : Promise.reject(r)))
      .then((data) => { if (alive) { setCreator(data); setNotFound(false); } })
      .catch(() => { if (alive) setNotFound(true); })
      .finally(() => { if (alive) setIsLoading(false); });
    return () => { alive = false; };
  }, [slug]);

  return { slug: slug || null, creator, isLoading, notFound };
}
