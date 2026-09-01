# ex02 — ACK et redélivrance

Simule un consumer JetStream. Lis `N`, puis des livraisons `sequence valeur outcome`, où `outcome`
vaut `OK` ou `FAIL`.

- `FAIL` : affiche `NAK sequence`, sans appliquer ni mémoriser ;
- premier `OK` de cette séquence : ajoute la valeur au total et affiche `ACK sequence` ;
- `OK` d'une séquence déjà appliquée : affiche `ACK_DUPLICATE sequence` sans ajouter la valeur.

Affiche enfin `TOTAL n`. Cette simulation matérialise pourquoi l'effet doit précéder l'ACK et rester
idempotent.
