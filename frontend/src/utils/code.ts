interface CodeBlock {
  code: string
  language: string
  isCode: boolean
  content: string
  isXRCode?: boolean
}


export const extractCodeBlocks = (content: string): CodeBlock[] => {
  const codeBlockRegex = /```(\w+)?\n([\s\S]*?)```/g;
  const blocks: CodeBlock[] = [];
  let lastIndex = 0;
  let match;

  while ((match = codeBlockRegex.exec(content)) !== null) {
    if (match.index > lastIndex) {
      blocks.push({
        isCode: false,
        content: content.slice(lastIndex, match.index),
        code: '',
        language: '',
      });
    }

    // Detect if the code block contains XR/WebGL-related keywords
    const isXRCode = /THREE|WebGLRenderer|AR|VR/.test(match[2]);

    blocks.push({
      isCode: true,
      content: match[0],
      code: match[2].trim(),
      language: match[1] || 'plaintext',
      isXRCode, // True if it's XR/WebGL code
    });


    // // Print statement to verify XR identification
    // if (isXRCode) {
    //   console.log("XR Code Detected:", match[2].trim());
    // } else {
    //   console.log("Non-XR Code:", match[2].trim());
    // }

    lastIndex = match.index + match[0].length; 
  }

  if (lastIndex < content.length) {
    blocks.push({
      isCode: false,
      content: content.slice(lastIndex),
      code: '',
      language: '',
    });
  }

  return blocks;
};
