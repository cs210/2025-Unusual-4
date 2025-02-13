import { marked } from 'marked';
import Prism from 'prismjs';

// Import additional Prism languages
import 'prismjs/components/prism-typescript';
import 'prismjs/components/prism-javascript';
import 'prismjs/components/prism-jsx';
import 'prismjs/components/prism-tsx';
import 'prismjs/components/prism-bash';
import 'prismjs/components/prism-json';
import 'prismjs/components/prism-markdown';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-css';
import 'prismjs/components/prism-sql';


export function configureMarked() {
  // Configure marked options
  marked.setOptions({
    gfm: true,
    breaks: true,
    pedantic: false,
    smartLists: true,
    smartypants: true
  });

  if (typeof window !== 'undefined' && !window.copyCode) {
    window.copyCode = (id: string) => {
      const codeBlock = document.getElementById(id);
      if (!codeBlock) return;

      const code = codeBlock.getAttribute('data-raw');
      if (!code) return;

      const decodedCode = decodeURIComponent(code);

      navigator.clipboard.writeText(decodedCode).then(() => {
        const button = codeBlock.parentElement?.querySelector('.copy-button');
        if (!button) return;

        const icon = button.querySelector('.copy-icon');
        if (icon) {
          icon.innerHTML = '<path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />';
        }
        button.classList.add('text-green-400');

        setTimeout(() => {
          if (icon) {
            icon.innerHTML = '<path d="M8 4v12a2 2 0 002 2h8a2 2 0 002-2V7.242a2 2 0 00-.602-1.43L16.083 2.57A2 2 0 0014.685 2H10a2 2 0 00-2 2z" /><path d="M16 18v2a2 2 0 01-2 2H6a2 2 0 01-2-2V9a2 2 0 012-2h2" />';
          }
          button.classList.remove('text-green-400');
        }, 2000);
      });
    };
  }

  const renderer = new marked.Renderer();

  // Properly render lists based on the actual format
  renderer.list = (list) => {
    const type = list.ordered ? 'ol' : 'ul';
    const items = list.items.map((item, index) => 
      `<li class="mb-1 flex items-start">
        ${item.task || item.checked ? '' : `<span class="mr-2">${list.ordered ? `${index + 1}. ` : '• '}</span>`}
        <span>${item.task 
          ? `<input type="checkbox" ${item.checked ? 'checked' : ''} disabled class="mt-1 mr-2" />${item.text}`
          : item.text
        }</span>
      </li>`
    ).join('');

    const startAttr = list.ordered && list.start ? ` start="${list.start}"` : '';
    return `<${type}${startAttr} class="pl-6 mb-4">${items}</${type}>`;
  };

  // Properly render headers based on the actual format
  renderer.heading = (heading) => {
    const depth = heading.depth;
    const text = heading.text;
    return `<h${depth} class="text-${4-depth}xl font-bold mb-4">${text}</h${depth}>`;
  };

  // Add blockquote renderer with more padding
  renderer.blockquote = (quote) => {
    // const cleanedText = quote.text.replace(/^>\s*/gm, '').trim();
    
    return `
      <blockquote class="border-l-4 border-gray-300 max-w-prose">
        <div class="pl-6 text-gray-600 italic">    ${quote.text}</div>
      </blockquote>
    `;
  
  };

  // Add link renderer
  renderer.link = (link) => {
    const href = link.href;
    const title = link.title ? ` title="${link.title}"` : '';
    const isExternal = href.startsWith('http');
    const externalAttrs = isExternal ? ' target="_blank" rel="noopener noreferrer"' : '';
    
    return `<a href="${href}"${title}${externalAttrs} class="text-blue-500 hover:text-blue-700 underline">${link.text}</a>`;
  };

  // Keep your original code block renderer
  renderer.code = (code) => {
    const language = code.lang || 'text';
    const codeId = `code-${Math.random().toString(36).substr(2, 9)}`;
    const codeText = code.text;

    try {
      const grammar = Prism.languages[language] || Prism.languages.text;
      const highlightedCode = Prism.highlight(codeText, grammar, language);

      return `
        <div class="code-block-wrapper group relative bg-[#1e1e1e] rounded-lg my-6">
          <div class="flex justify-between items-center h-8 px-4 border-b border-gray-700">
            <span class="text-xs text-gray-400 leading-none">${language}</span>
            <button 
              onclick="copyCode('${codeId}')"
              class="copy-button flex items-center gap-1 text-xs text-gray-400 hover:text-gray-200 transition-colors leading-none"
              aria-label="Copy code"
            >
              <svg class="copy-icon w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M8 4v12a2 2 0 002 2h8a2 2 0 002-2V7.242a2 2 0 00-.602-1.43L16.083 2.57A2 2 0 0014.685 2H10a2 2 0 00-2 2z" />
                <path d="M16 18v2a2 2 0 01-2 2H6a2 2 0 01-2-2V9a2 2 0 012-2h2" />
              </svg>
              <span>Copy</span>
            </button>
          </div>
          <div class="overflow-x-auto scrollbar-thin scrollbar-thumb-gray-700 scrollbar-track-gray-900">
            <pre class="p-0 m-0"><code id="${codeId}" class="language-${language} text-sm block p-4" data-raw="${encodeURIComponent(codeText)}">${highlightedCode}</code></pre>
          </div>
        </div>
      `;
    } catch (error) {
      console.error('Error highlighting code:', error);
      return `
        <div class="code-block-wrapper bg-[#1e1e1e] rounded-lg my-6">
          <div class="overflow-x-auto scrollbar-thin scrollbar-thumb-gray-700 scrollbar-track-gray-900">
            <pre class="m-0 px-8 py-6"><code class="text-sm">${codeText}</code></pre>
          </div>
        </div>
      `;
    }
  };

  marked.use({ renderer });
}