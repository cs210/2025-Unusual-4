'use client';

import * as THREE from 'three';
import { useEffect, useRef } from 'react';

// Ensure a global registry exists
window.sceneRegistry = window.sceneRegistry || {};

interface SceneViewProps {
  code: string;
}

const SceneView = ({ code }: SceneViewProps) => {
  const containerRef = useRef<HTMLDivElement>(null);

  // Persistent refs for scene, camera, and renderer
  const sceneRef = useRef<THREE.Scene | null>(null);
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    // Create the scene, camera, and renderer once
    if (!sceneRef.current) {
      sceneRef.current = new THREE.Scene();
      cameraRef.current = new THREE.PerspectiveCamera(
        75,
        containerRef.current.clientWidth / containerRef.current.clientHeight,
        0.1,
        1000
      );
      rendererRef.current = new THREE.WebGLRenderer({ antialias: true });
      rendererRef.current.setSize(
        containerRef.current.clientWidth,
        containerRef.current.clientHeight
      );
      containerRef.current.appendChild(rendererRef.current.domElement);

      // Start the animation loop
      const animate = () => {
        requestAnimationFrame(animate);
        if (rendererRef.current && sceneRef.current && cameraRef.current) {
          rendererRef.current.render(sceneRef.current, cameraRef.current);
        }
      };
      animate();
    }

    const scene = sceneRef.current!;
    const camera = cameraRef.current!;
    const renderer = rendererRef.current!;

    // Ensure the global registry exists
    if (!window.sceneRegistry) {
      window.sceneRegistry = {};
    }

    // Define a generic helper function for create-or-update logic
    function updateOrCreate(
      name: string,
      createFn: () => THREE.Object3D,
      updateFn: (object: THREE.Object3D) => void
    ) {
      let object = window.sceneRegistry[name] || scene.getObjectByName(name);
      if (object) {
        updateFn(object);
      } else {
        object = createFn();
        if (!object) {
          console.error("Error: createFn did not return a valid object for", name);
          return;
        }
        object.name = name;
        scene.add(object);
        window.sceneRegistry[name] = object;
      }
      return object;
    }
    

    // Execute the injected code with context that includes the helper
    try {
      const executeCode = new Function(
        'context',
        `
          try {
            with(context) {
              ${code}
            }
          } catch (error) {
            console.error('Execution Error:', error);
          }
        `
      );

      executeCode({
        THREE,
        scene,
        camera,
        renderer,
        updateOrCreate,
      });
    } catch (error) {
      console.error('Error executing WebGL code:', error);
    }
  }, [code]);

  return <div ref={containerRef} className="w-full h-96 bg-black rounded-xl"></div>;
};

export default SceneView;
