'use client';

import * as THREE from 'three';

import { useEffect, useRef } from 'react';

interface SceneViewProps {
  code: string;
}

// starter SceneView code ---probably doesn't work
const SceneView = ({ code }: SceneViewProps) => {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    // Initialize Three.js Scene
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(
      75,
      containerRef.current.clientWidth / containerRef.current.clientHeight,
      0.1,
      1000
    );
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight);

    containerRef.current.appendChild(renderer.domElement);

    try {
      const executeCode = new Function(
        'THREE',
        'scene',
        'camera',
        'renderer',
        code
      );

      executeCode(THREE, scene, camera, renderer);
    } catch (error) {
      console.error('Error executing WebGL code:', error);
    }

    const animate = () => {
      requestAnimationFrame(animate);
      renderer.render(scene, camera);
    };

    animate();

    return () => {
      renderer.dispose();
      containerRef.current?.removeChild(renderer.domElement);
    };
  }, [code]);

  return <div ref={containerRef} className="w-full h-96 bg-black rounded-xl"></div>;
};

export default SceneView;
