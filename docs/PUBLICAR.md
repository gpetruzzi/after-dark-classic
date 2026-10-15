# Preparación de la publicación

El directorio local contiene código y materiales de terceros en `package/`. El aviso no autoriza por sí solo a publicar esos materiales.

1. Decidir qué licencia se aplicará únicamente al código nuevo.
2. Verificar los permisos para los archivos originales antes de subir `package/`, las capturas que los reproducen o el instalador completo. Si no se incluyen, excluir esas rutas de la publicación y mantener las instrucciones para aportarlos localmente.
3. Crear un repositorio público llamado, por ejemplo, `after-dark-classic`.
4. Subir los archivos descomprimidos que se hayan aprobado para publicar, con `README.md` y `NOTICE.md` en la raíz.
5. Revisar el contenido antes del primer commit público.
6. Si corresponde publicar el instalador completo, crear una Release preliminar `v0.5.0` y adjuntar el instalador autorizado. No es necesario guardar ejecutables generados en el historial del código.

Referencias:

- [Crear repositorio](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)
- [Licencias de repositorios](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
- [GitHub Terms of Service](https://docs.github.com/en/site-policy/github-terms/github-terms-of-service)
