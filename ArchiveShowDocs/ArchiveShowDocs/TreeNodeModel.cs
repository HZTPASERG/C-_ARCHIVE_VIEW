﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveShowDocs
{
    public class TreeNodeModel
    {
        public string Table { get; set; }      // Nombre de la tabla
        public int OwnerId { get; set; }      // Propietario
        public int Key { get; set; }          // Clave única
        public string Name { get; set; }      // Nombre del nodo
        public int ImageId { get; set; }      // ID de la imagen asociada
        public string Id { get; set; }        // ID del nodo
        public string ParentId { get; set; }  // ID del nodo padre
        public int Rank { get; set; }         // Nivel jerárquico o rango
    }
}