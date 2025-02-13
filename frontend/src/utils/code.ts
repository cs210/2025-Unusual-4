interface CodeBlock {
  code: string
  language: string
  isCode: boolean
  content: string
}

export const extractCodeBlocks = (content: string): CodeBlock[] => {
  const codeBlockRegex = /```(\w+)?\n([\s\S]*?)```/g
  const blocks: CodeBlock[] = []
  let lastIndex = 0
  let match

  while ((match = codeBlockRegex.exec(content)) !== null) {
    // Add text before code block
    if (match.index > lastIndex) {
      blocks.push({
        isCode: false,
        content: content.slice(lastIndex, match.index),
        code: '',
        language: ''
      })
    }

    // Add code block
    blocks.push({
      isCode: true,
      content: match[0],
      code: match[2].trim(),
      language: match[1] || 'plaintext'
    })

    lastIndex = match.index + match[0].length
  }

  // Add remaining text
  if (lastIndex < content.length) {
    blocks.push({
      isCode: false,
      content: content.slice(lastIndex),
      code: '',
      language: ''
    })
  }

  return blocks
} 