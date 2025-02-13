'use client'

import { useState } from 'react'
import { CopyToClipboard } from 'react-copy-to-clipboard'
import { motion } from 'framer-motion'

interface CodeViewProps {
  code: string
  language: string
}

const CodeView = ({ code, language }: CodeViewProps) => {
  const [copied, setCopied] = useState(false)
  // const isRenderable = ['jsx', 'tsx', 'html'].includes(language.toLowerCase())

  const handleCopy = () => {
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="border-b border-slate-700"
    >
      <div className="flex items-center justify-between p-4 bg-slate-900">
        <span className="text-sm text-slate-400">{language}</span>
        <CopyToClipboard text={code} onCopy={handleCopy}>
          <button className="text-sm text-slate-400 hover:text-white rounded-lg px-2 py-1 hover:bg-slate-800 transition-colors">
            {copied ? 'Copied!' : 'Copy code'}
          </button>
        </CopyToClipboard>
      </div>

      <div className="p-4 bg-slate-900/50">
        <pre className="text-white rounded-lg overflow-auto">
          <code>{code}</code>
        </pre>
      </div>
    </motion.div>
  )
}

export default CodeView 