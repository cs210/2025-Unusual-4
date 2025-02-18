'use client'

import { useState, useEffect } from 'react'
import { CopyToClipboard } from 'react-copy-to-clipboard'
import { motion } from 'framer-motion'

interface CodeViewProps {
  code: string
  currentLanguage: string
  onLanguageChange?: (newLanguage: string, newCode: string) => void
}

const CodeView = ({ code: initialCode, currentLanguage, onLanguageChange = (newLanguage: string, newCode: string) => {} }: CodeViewProps) => {
  const [copied, setCopied] = useState(false)
  const [currentCode, setCurrentCode] = useState(initialCode)
  const [isConverting, setIsConverting] = useState(false)

  // Update currentCode when initialCode changes
  useEffect(() => {
    setCurrentCode(initialCode)
  }, [initialCode])

  const languages = [
    'JavaScript',
    'C#',
    'C++',
    'Python',
    'Unity (C#)',
    'Unreal (C++)',
    'Three.js',
    'WebXR',
    'ARKit (Swift)',
    'ARCore (Java/Kotlin)'
  ]

  const handleCopy = () => {
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleLanguageChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newLanguage = e.target.value
    console.log('Changing language from', currentLanguage, 'to', newLanguage);
    setIsConverting(true)

    try {
      const response = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          message: `Convert this code from ${currentLanguage} to ${newLanguage}:\n\n${currentCode}`,
          messages: []
        }),
      });

      if (!response.ok) throw new Error('Failed to convert code');
      
      const reader = response.body?.getReader();
      if (!reader) throw new Error('No reader available');

      let fullResponse = '';
      const decoder = new TextDecoder();

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        const chunk = decoder.decode(value);
        fullResponse += chunk;
      }

      const codeMatch = fullResponse.match(/```[\w]*\n([\s\S]*?)```/);
      const convertedCode = codeMatch ? codeMatch[1].trim() : fullResponse.trim();
      
      console.log('Code converted successfully');
      setCurrentCode(convertedCode);
      
      // Only call onLanguageChange if it exists
      if (typeof onLanguageChange === 'function') {
        onLanguageChange(newLanguage, convertedCode);
      }
      
    } catch (error) {
      console.error('Error converting code:', error);
    } finally {
      setIsConverting(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="border-b border-slate-700"
    >
      <div className="flex items-center justify-between p-4 bg-slate-900">
        <div className="flex items-center gap-4">
          <select
            value={currentLanguage}
            onChange={handleLanguageChange}
            className="bg-slate-800 text-slate-400 text-sm rounded-lg px-3 py-1.5 border border-slate-700 hover:border-slate-600 focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={isConverting}
          >
            {languages.map((lang) => (
              <option key={lang} value={lang}>
                {lang}
              </option>
            ))}
          </select>
          {isConverting && (
            <span className="text-sm text-slate-400">Converting...</span>
          )}
        </div>
        <CopyToClipboard text={currentCode} onCopy={handleCopy}>
          <button className="text-sm text-slate-400 hover:text-white rounded-lg px-2 py-1 hover:bg-slate-800 transition-colors">
            {copied ? 'Copied!' : 'Copy code'}
          </button>
        </CopyToClipboard>
      </div>

      <div className="p-4 bg-slate-900/50">
        <pre className="text-white rounded-lg overflow-auto">
          <code>{currentCode}</code>
        </pre>
      </div>
    </motion.div>
  )
}

export default CodeView 